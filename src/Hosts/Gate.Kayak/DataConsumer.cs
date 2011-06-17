using System;
using Kayak;

namespace Gate.Kayak
{
    class DataConsumer : IDataConsumer
    {
        Func<ArraySegment<byte>, Action, bool> onData;
        Action<Exception> onError;
        Action onEnd;

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
