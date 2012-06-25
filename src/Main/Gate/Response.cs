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
        Action<ResultParameters, Exception> _callback;
        ResultParameters _result;

        int _autostart;
        readonly Object _onStartSync = new object();
        Action _onStart = () => { };

        Func<ArraySegment<byte>, Action<Exception>, Owin.TempEnum> _responseWrite;
        Action<Exception> _responseEnd;
        CancellationToken _responseCancellationToken = CancellationToken.None;
        Stream _outputStream;

        public Response(Action<ResultParameters, Exception> callback)
            : this(callback, 200)
        {
        }

        public Response(Action<ResultParameters, Exception> callback, int statusCode)
            : this(callback, statusCode, new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase))
        {
        }

        public Response(Action<ResultParameters, Exception> callback, int statusCode, IDictionary<string, string[]> headers)
            : this(callback, statusCode, headers, new Dictionary<string, object>())
        {
        }

        public Response(Action<ResultParameters, Exception> callback, int statusCode, IDictionary<string, string[]> headers, IDictionary<string, object> properties)
        {
            _callback = callback;
            _result = new ResultParameters
            {
                Status = statusCode,
                Headers = headers,
                Body = ResponseBody,
                Properties = properties
            };

            _responseWrite = EarlyResponseWrite;
            _responseEnd = EarlyResponseEnd;

            Encoding = Encoding.UTF8;
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


        public Response Start()
        {
            _autostart = 1;
            Interlocked.Exchange(ref _callback, ResultCalledAlready).Invoke(_result, null);
            return this;
        }

        public Response Start(string status)
        {
            if (!string.IsNullOrWhiteSpace(status))
                Status = status;

            return Start();
        }

        public Response Start(string status, IEnumerable<KeyValuePair<string, string[]>> headers)
        {
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    Headers[header.Key] = header.Value;
                }
            }
            return Start(status);
        }

        public void Start(string status, IEnumerable<KeyValuePair<string, string>> headers)
        {
            var actualHeaders = headers.Select(kv => new KeyValuePair<string, string[]>(kv.Key, new[] { kv.Value }));
            Start(status, actualHeaders);
        }

        public void Start(Action continuation)
        {
            OnStart(continuation);
            Start();
        }

        public void Start(string status, Action continuation)
        {
            OnStart(continuation);
            Start(status);
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

        public Owin.TempEnum Write(string text)
        {
            // this could be more efficient if it spooled the immutable strings instead...
            var data = Encoding.GetBytes(text);
            return Write(new ArraySegment<byte>(data));
        }

        public Owin.TempEnum Write(string format, params object[] args)
        {
            return Write(string.Format(format, args));
        }

        public Owin.TempEnum Write(ArraySegment<byte> data)
        {
            return _responseWrite(data, null);
        }

        public Owin.TempEnum Write(ArraySegment<byte> data, Action<Exception> callback)
        {
            return _responseWrite(data, callback);
        }

        public Task WriteAsync(ArraySegment<byte> data)
        {
            return (Task)BeginWrite(data, null, null);
        }

        public IAsyncResult BeginWrite(ArraySegment<byte> data, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<object>();
            Action<Exception> continuation = error =>
            {
                if (error != null)
                {
                    tcs.SetException(error);
                }
                else
                {
                    tcs.SetResult(null);
                }
            };
            if (_responseWrite.Invoke(data, continuation) == OwinConstants.CompletedSynchronously)
            {
                tcs.SetResult(null);
            }
            if (callback != null)
            {
                tcs.Task.Finally(() => callback(tcs.Task));
            }
            return tcs.Task;
        }

        public void EndWrite(IAsyncResult result)
        {
            ((Task)result).Wait();
        }


        public Owin.TempEnum Flush()
        {
            return Write(default(ArraySegment<byte>));
        }

        public Owin.TempEnum Flush(Action<Exception> callback)
        {
            return Write(default(ArraySegment<byte>), callback);
        }

        public Task FlushAsync()
        {
            return WriteAsync(default(ArraySegment<byte>));
        }

        public IAsyncResult BeginFlush(AsyncCallback callback, object state)
        {
            return BeginWrite(default(ArraySegment<byte>), callback, state);
        }

        public void EndFlush(IAsyncResult result)
        {
            EndWrite(result);
        }

        public void End()
        {
            OnEnd(null);
        }

        public void End(string text)
        {
            Write(text);
            OnEnd(null);
        }

        public void End(ArraySegment<byte> data)
        {
            Write(data);
            OnEnd(null);
        }

        public void Error(Exception error)
        {
            OnEnd(error);
        }

        void ResponseBody(
            Func<ArraySegment<byte>, Action<Exception>, Owin.TempEnum> write,
            Action<Exception> end,
            CancellationToken cancellationToken)
        {
            _responseWrite = write;
            _responseEnd = end;
            _responseCancellationToken = cancellationToken;
            lock (_onStartSync)
            {
                Interlocked.Exchange(ref _onStart, null).Invoke();
            }
        }


        static readonly Action<ResultParameters, Exception> ResultCalledAlready =
            (result, error) =>
            {
                throw new InvalidOperationException("Start must only be called once on a Response and it must be called before Write or End");
            };


        void Autostart()
        {
            if (Interlocked.Increment(ref _autostart) == 1)
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
            Interlocked.Exchange(ref _responseEnd, _ => { }).Invoke(error);
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

        Owin.TempEnum EarlyResponseWrite(ArraySegment<byte> data, Action<Exception> callback)
        {
            var copy = data;
            if (copy.Count != 0)
            {
                copy = new ArraySegment<byte>(new byte[data.Count], 0, data.Count);
                Array.Copy(data.Array, data.Offset, copy.Array, 0, data.Count);
            }
            OnStart(
                () =>
                {
                    var willCallback = _responseWrite(copy, callback);
                    if (callback != null && willCallback == OwinConstants.CompletingAsynchronously)
                    {
                        callback.Invoke(null);
                    }
                });
            if (!Buffer || data.Array == null)
            {
                Autostart();
            }
            return OwinConstants.CompletingAsynchronously;
        }


        void EarlyResponseEnd(Exception ex)
        {
            OnStart(() => OnEnd(ex));
            Autostart();
        }
    }
}