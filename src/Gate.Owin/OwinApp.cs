using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gate.Owin
{
    public delegate Task<OwinResult> OwinApp(IDictionary<string, object> env);

    public class OwinResult : Tuple<string, IDictionary<String, String>, IObservable<OwinData>>
    {
        public OwinResult(string status, IDictionary<string, string> headers, IObservable<OwinData> body)
            : base(status, headers, body) { }

        public string Status { get { return Item1; } }
        public IDictionary<string, string> Headers { get { return Item2; } }
        public IObservable<OwinData> Body { get { return Item3; } }
    }

    public class OwinData : Tuple<ArraySegment<byte>, Action, Action>
    {
        public OwinData(ArraySegment<byte> bytes, Action pause, Action resume)
            : base(bytes, pause, resume) { }

        public ArraySegment<byte> Bytes { get { return Item1; } }
        public Action Pause { get { return Item2; } }
        public Action Resume { get { return Item3; } }
    }
}