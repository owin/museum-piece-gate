using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Gate.Owin;
using Gate.Utils;

namespace Gate
{
    public class Response
    {
        ResultDelegate _result;

        int _autostart;
        readonly Object _onStartSync = new object();
        Action _onStart = () => { };

        Func<ArraySegment<byte>, bool> _responseWrite;
        Func<Action, bool> _responseFlush;
        Action<Exception> _responseEnd;
        CancellationToken _responseCancellationToken = CancellationToken.None;

        public Response(ResultDelegate result)
            : this(result, "200 OK")
        {
        }

        public Response(ResultDelegate result, string status)
            : this(result, status, new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase))
        {
        }

        public Response(ResultDelegate result, string status, IDictionary<string, IEnumerable<string>> headers)
        {
            _result = result;

            _responseWrite = EarlyResponseWrite;
            _responseFlush = EarlyResponseFlush;
            _responseEnd = EarlyResponseEnd;

            Status = status;
            Headers = headers;
            Encoding = Encoding.UTF8;
        }

        public string Status { get; set; }
        public IDictionary<string, IEnumerable<string>> Headers { get; set; }
        public Encoding Encoding { get; set; }
        public bool Buffer { get; set; }

        string GetHeader(string name)
        {
            var values = GetHeaders(name);
            if (values == null)
            {
                return null;
            }

            if (values is string[])
            {
                var valueArray = (string[])values;
                switch (valueArray.Length)
                {
                    case 0:
                        return string.Empty;
                    case 1:
                        return valueArray[0];
                    default:
                        return string.Join(",", valueArray);
                }
            }

            var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext())
                return string.Empty;

            var string1 = enumerator.Current;
            if (!enumerator.MoveNext())
                return string1;

            var string2 = enumerator.Current;
            if (!enumerator.MoveNext())
                return string1 + "," + string2;

            var sb = new StringBuilder(string1 + "," + string2 + "," + enumerator.Current);
            while (enumerator.MoveNext())
            {
                sb.Append(',');
                sb.Append(enumerator.Current);
            }
            return sb.ToString();
        }

        IEnumerable<string> GetHeaders(string name)
        {
            IEnumerable<string> value;
            return Headers.TryGetValue(name, out value) ? value : null;
        }

        void SetHeader(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                Headers.Remove(value);
            else
                Headers[name] = new[] { value };
        }

        public string ContentType
        {
            get { return GetHeader("Content-Type"); }
            set { SetHeader("Content-Type", value); }
        }


        public Response Start()
        {
            _autostart = 1;
            Interlocked.Exchange(ref _result, ResultCalledAlready).Invoke(Status, Headers, ResponseBody);
            return this;
        }

        public Response Start(string status)
        {
            if (!string.IsNullOrWhiteSpace(status))
                Status = status;

            return Start();
        }

        public Response Start(string status, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
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
            var actualHeaders = headers.Select(kv => new KeyValuePair<string, IEnumerable<string>>(kv.Key, new[] { kv.Value }));
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

        public Response Write(string text)
        {
            // this could be more efficient if it spooled the immutable strings instead...
            var data = Encoding.GetBytes(text);
            return Write(new ArraySegment<byte>(data));
        }

        public Response Write(string format, params object[] args)
        {
            return Write(string.Format(format, args));
        }

        public Response Write(ArraySegment<byte> data)
        {
            _responseWrite(data);
            return this;
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
            Func<ArraySegment<byte>, bool> write,
            Func<Action, bool> flush,
            Action<Exception> end,
            CancellationToken cancellationToken)
        {
            _responseWrite = write;
            _responseFlush = flush;
            _responseEnd = end;
            _responseCancellationToken = cancellationToken;
            lock (_onStartSync)
            {
                Interlocked.Exchange(ref _onStart, null).Invoke();
            }
        }


        static readonly ResultDelegate ResultCalledAlready =
            (_, __, ___) =>
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

        bool EarlyResponseWrite(ArraySegment<byte> data)
        {
            var copy = new byte[data.Count];
            Array.Copy(data.Array, data.Offset, copy, 0, data.Count);
            OnStart(() => _responseWrite(new ArraySegment<byte>(copy)));
            if (!Buffer)
            {
                Autostart();
            }
            return true;
        }


        bool EarlyResponseFlush(Action drained)
        {
            OnStart(() =>
            {
                if (!_responseFlush.Invoke(drained))
                {
                    drained.Invoke();
                }
            });
            Autostart();
            return true;
        }

        void EarlyResponseEnd(Exception ex)
        {
            OnStart(() => OnEnd(ex));
            Autostart();
        }
    }
}