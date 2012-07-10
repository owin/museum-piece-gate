using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Owin;
using Gate.Utils;

namespace Gate
{
    public class Response
    {
        Action<ResultParameters, Exception> _startCalled;
        ResultParameters _result;

        int _autostart;
        readonly Object _onStartSync = new object();
        Action _onStart = () => { };

        TaskCompletionSource<ResultParameters> _callCompletionSource = new TaskCompletionSource<ResultParameters>();

        CancellationToken _responseCancellationToken = CancellationToken.None;
        TaskCompletionSource<object> _responseCompletionSource = new TaskCompletionSource<object>();

        Stream _outputStream;


        public Response()
            : this(200)
        {
        }

        public Response(int statusCode)
            : this(statusCode, new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase))
        {
        }

        public Response(int statusCode, IDictionary<string, string[]> headers)
            : this(statusCode, headers, new Dictionary<string, object>())
        {
        }

        public Response(int statusCode, IDictionary<string, string[]> headers, IDictionary<string, object> properties)
        {
            _startCalled = StartCalled;
            _result = new ResultParameters
            {
                Status = statusCode,
                Headers = headers,
                Body = ResponseBodyAsync,
                Properties = properties
            };

            _responseWrite = EarlyResponseWrite;
            _responseWriteAsync = EarlyResponseWriteAsync;
            _responseFlush = EarlyResponseFlush;
            _responseFlushAsync = EarlyResponseFlushAsync;

            Encoding = Encoding.UTF8;
        }

        internal Func<Task<ResultParameters>> Next { get; set; }

        public void Skip()
        {
            Next.Invoke().CopyResultToCompletionSource(_callCompletionSource);
        }

        public Task<ResultParameters> GetResultAsync()
        {
            return _callCompletionSource.Task;
        }

        public IDictionary<string, string[]> Headers
        {
            get { return _result.Headers; }
            set { _result.Headers = value; }
        }
        public IDictionary<string, object> Properties
        {
            get { return _result.Properties; }
            set { _result.Properties = value; }
        }

        public Encoding Encoding { get; set; }
        public bool Buffer { get; set; }


        public string Status
        {
            get
            {
                var reasonPhrase = ReasonPhrase;
                return string.IsNullOrEmpty(reasonPhrase)
                    ? StatusCode.ToString(CultureInfo.InvariantCulture)
                    : StatusCode.ToString(CultureInfo.InvariantCulture) + " " + reasonPhrase;
            }
            set
            {
                if (value.Length < 3 || (value.Length >= 4 && value[3] != ' '))
                {
                    throw new ArgumentException("Status must be a string with 3 digit statuscode, a space, and a reason phrase");
                }
                _result.Status = int.Parse(value.Substring(0, 3));
                ReasonPhrase = value.Length < 4 ? null : value.Substring(4);
            }
        }

        public int StatusCode
        {
            get
            {
                return _result.Status;
            }
            set
            {
                if (_result.Status != value)
                {
                    _result.Status = value;
                    ReasonPhrase = null;
                }
            }
        }

        public string ReasonPhrase
        {
            get
            {
                object value;
                var reasonPhrase = Properties.TryGetValue("owin.ReasonPhrase", out value) ? Convert.ToString(value) : null;
                return string.IsNullOrEmpty(reasonPhrase) ? ReasonPhrases.ToReasonPhrase(StatusCode) : reasonPhrase;
            }
            set { Properties["owin.ReasonPhrase"] = value; }
        }


        public string GetHeader(string name)
        {
            var values = GetHeaders(name);
            if (values == null)
            {
                return null;
            }

            switch (values.Length)
            {
                case 0:
                    return string.Empty;
                case 1:
                    return values[0];
                default:
                    return string.Join(",", values);
            }
        }

        public string[] GetHeaders(string name)
        {
            string[] existingValues;
            return Headers.TryGetValue(name, out existingValues) ? existingValues : null;
        }

        public Response SetHeader(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                Headers.Remove(value);
            else
                Headers[name] = new[] { value };
            return this;
        }

        public Response SetCookie(string key, string value)
        {
            Headers.AddHeader("Set-Cookie", Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value) + "; path=/");
            return this;
        }

        public Response SetCookie(string key, Cookie cookie)
        {
            var domainHasValue = !string.IsNullOrEmpty(cookie.Domain);
            var pathHasValue = !string.IsNullOrEmpty(cookie.Path);
            var expiresHasValue = cookie.Expires.HasValue;

            var setCookieValue = string.Concat(
                Uri.EscapeDataString(key),
                "=",
                Uri.EscapeDataString(cookie.Value ?? ""), //TODO: concat complex value type with '&'?
                !domainHasValue ? null : "; domain=",
                !domainHasValue ? null : cookie.Domain,
                !pathHasValue ? null : "; path=",
                !pathHasValue ? null : cookie.Path,
                !expiresHasValue ? null : "; expires=",
                !expiresHasValue ? null : cookie.Expires.Value.ToString("ddd, dd-MMM-yyyy HH:mm:ss ") + "GMT",
                !cookie.Secure ? null : "; secure",
                !cookie.HttpOnly ? null : "; HttpOnly"
                );
            Headers.AddHeader("Set-Cookie", setCookieValue);
            return this;
        }

        public Response DeleteCookie(string key)
        {
            Func<string, bool> predicate = value => value.StartsWith(key + "=", StringComparison.InvariantCultureIgnoreCase);

            var deleteCookies = new[] { Uri.EscapeDataString(key) + "=; expires=Thu, 01-Jan-1970 00:00:00 GMT" };
            var existingValues = Headers.GetHeaders("Set-Cookie");
            if (existingValues == null)
            {
                Headers["Set-Cookie"] = deleteCookies;
                return this;
            }

            Headers["Set-Cookie"] = existingValues.Where(value => !predicate(value)).Concat(deleteCookies).ToArray();
            return this;
        }

        public Response DeleteCookie(string key, Cookie cookie)
        {
            var domainHasValue = !string.IsNullOrEmpty(cookie.Domain);
            var pathHasValue = !string.IsNullOrEmpty(cookie.Path);

            Func<string, bool> rejectPredicate;
            if (domainHasValue)
            {
                rejectPredicate = value =>
                    value.StartsWith(key + "=", StringComparison.InvariantCultureIgnoreCase) &&
                        value.IndexOf("domain=" + cookie.Domain, StringComparison.InvariantCultureIgnoreCase) != -1;
            }
            else if (pathHasValue)
            {
                rejectPredicate = value =>
                    value.StartsWith(key + "=", StringComparison.InvariantCultureIgnoreCase) &&
                        value.IndexOf("path=" + cookie.Path, StringComparison.InvariantCultureIgnoreCase) != -1;
            }
            else
            {
                rejectPredicate = value => value.StartsWith(key + "=", StringComparison.InvariantCultureIgnoreCase);
            }
            var existingValues = Headers.GetHeaders("Set-Cookie");
            if (existingValues != null)
            {
                Headers["Set-Cookie"] = existingValues.Where(value => !rejectPredicate(value)).ToArray();
            }

            return SetCookie(key, new Cookie
            {
                Path = cookie.Path,
                Domain = cookie.Domain,
                Expires = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            });
        }


        public class Cookie
        {
            public Cookie()
            {
                Path = "/";
            }
            public Cookie(string value)
            {
                Path = "/";
                Value = value;
            }
            public string Value { get; set; }
            public string Domain { get; set; }
            public string Path { get; set; }
            public DateTime? Expires { get; set; }
            public bool Secure { get; set; }
            public bool HttpOnly { get; set; }
        }

        public string ContentType
        {
            get { return GetHeader("Content-Type"); }
            set { SetHeader("Content-Type", value); }
        }


        public void Start()
        {
            _autostart = 1;
            Interlocked.Exchange(ref _startCalled, StartCalledAlready).Invoke(_result, null);
        }

        public Task StartAsync()
        {
            var tcs = new TaskCompletionSource<object>();
            OnStart(() => tcs.SetResult(null));
            Start();
            return tcs.Task;
        }


        public Stream OutputStream
        {
            get
            {
                if (_outputStream == null)
                {
                    Interlocked.Exchange(ref _outputStream, new ResponseStream(this));
                }
                return _outputStream;
            }
        }

        public void Write(string text)
        {
            // this could be more efficient if it spooled the immutable strings instead...
            var data = Encoding.GetBytes(text);
            Write(data);
        }

        public void Write(string format, params object[] args)
        {
            Write(string.Format(format, args));
        }


        public void Write(byte[] buffer)
        {
            _responseWrite(buffer, 0, buffer.Length);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            _responseWrite(buffer, offset, count);
        }

        public void Write(ArraySegment<byte> data)
        {
            _responseWrite(data.Array, data.Offset, data.Count);
        }

        public Task WriteAsync(byte[] buffer, int offset, int count)
        {
            return _responseWriteAsync(buffer, offset, count);
        }

        public Task WriteAsync(ArraySegment<byte> data)
        {
            return _responseWriteAsync(data.Array, data.Offset, data.Count);
        }

        public IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<object>(state);
            _responseWriteAsync(buffer, offset, count).CopyResultToCompletionSource(tcs, null);
            return tcs.Task;
        }

        public void EndWrite(IAsyncResult result)
        {
            ((Task)result).Wait();
        }


        public void Flush()
        {
            _responseFlush();
        }

        public Task FlushAsync()
        {
            return _responseFlushAsync();
        }

        public IAsyncResult BeginFlush(AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<object>(state);
            _responseFlushAsync().CopyResultToCompletionSource(tcs, null);
            return tcs.Task;
        }

        public void EndFlush(IAsyncResult result)
        {
            ((Task)result).Wait();
        }

        public void End()
        {
            OnEnd(null);
        }

        public void Error(Exception error)
        {
            OnEnd(error);
        }

        Task ResponseBodyAsync(
            Stream output,
            CancellationToken cancellationToken)
        {
            _responseCancellationToken = cancellationToken;

            _responseWrite = output.Write;
            _responseWriteAsync = output.WriteAsync;
            _responseFlush = output.Flush;
            _responseFlushAsync = output.FlushAsync;

            lock (_onStartSync)
            {
                Interlocked.Exchange(ref _onStart, null).Invoke();
            }

            return _responseCompletionSource.Task;
        }


        void StartCalled(ResultParameters result, Exception ex)
        {
            if (ex != null)
            {
                _callCompletionSource.SetException(ex);
            }
            else
            {
                _callCompletionSource.SetResult(result);
            }
        }

        static readonly Action<ResultParameters, Exception> StartCalledAlready =
            (result, error) =>
            {
                throw new InvalidOperationException("Start must only be called once on a Response and it must be called before Write or End");
            };


        void Autostart()
        {
            if (Buffer == false && Interlocked.Increment(ref _autostart) == 1)
            {
                Start();
            }
        }


        void OnStart(Action notify)
        {
            lock (_onStartSync)
            {
                if (_onStart != null)
                {
                    var prior = _onStart;
                    _onStart = () =>
                    {
                        prior.Invoke();
                        CallNotify(notify);
                    };
                    return;
                }
            }
            CallNotify(notify);
        }

        void OnEnd(Exception error)
        {
            if (error != null)
            {
                _responseCompletionSource.SetException(error);
            }
            else
            {
                _responseCompletionSource.SetResult(null);
            }
        }

        void CallNotify(Action notify)
        {
            try
            {
                notify.Invoke();
            }
            catch (Exception ex)
            {
                Error(ex);
            }
        }


        Action<byte[], int, int> _responseWrite;
        void EarlyResponseWrite(byte[] buffer, int offset, int count)
        {
            var copy = buffer;
            if (count != 0)
            {
                copy = new byte[count];
                Array.Copy(buffer, offset, copy, 0, count);
            }
            OnStart(() => _responseWrite(copy, 0, count));
            Autostart();
        }

        Func<byte[], int, int, Task> _responseWriteAsync;
        Task EarlyResponseWriteAsync(byte[] buffer, int offset, int count)
        {
            var tcs = new TaskCompletionSource<object>();
            OnStart(() => _responseWriteAsync(buffer, offset, count).CopyResultToCompletionSource(tcs, null));
            Autostart();
            return tcs.Task;
        }

        Action _responseFlush;
        void EarlyResponseFlush()
        {
            OnStart(() => _responseFlush());
            Autostart();
        }

        Func<Task> _responseFlushAsync;
        Task EarlyResponseFlushAsync()
        {
            var tcs = new TaskCompletionSource<object>();
            OnStart(() => _responseFlushAsync().CopyResultToCompletionSource(tcs, null));
            Autostart();
            return tcs.Task;
        }
    }
}