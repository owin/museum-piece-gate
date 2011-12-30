using System;
using Kayak;

namespace Gate.Hosts.Kayak
{
    class DataConsumer : IDataConsumer
    {
        readonly Func<ArraySegment<byte>, Action, bool> onData;
        readonly Action<Exception> onError;
        readonly Action onEnd;

        public DataConsumer(
            Func<ArraySegment<byte>, Action, bool> onData,
            Action<Exception> onError,
            Action onEnd)
        {
            this.onData = onData;
            this.onError = onError;
            this.onEnd = onEnd;
        }

        public bool OnData(ArraySegment<byte> data, Action continuation)
        {
            return onData(data, continuation);
        }

        public void OnEnd()
        {
            onEnd();
        }

        public void OnError(Exception e)
        {
            onError(e);
        }
    }
}
