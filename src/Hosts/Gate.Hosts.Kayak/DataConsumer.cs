using System;
using Kayak;
using Owin;

namespace Gate.Hosts.Kayak
{
    class DataConsumer : IDataConsumer
    {
        readonly Func<ArraySegment<byte>, Action<Exception>, TempEnum> onData;
        readonly Action<Exception> onError;
        readonly Action onEnd;

        public DataConsumer(
            Func<ArraySegment<byte>, Action<Exception>, TempEnum> onData,
            Action<Exception> onError,
            Action onEnd)
        {
            this.onData = onData;
            this.onError = onError;
            this.onEnd = onEnd;
        }

        public bool OnData(ArraySegment<byte> data, Action continuation)
        {
            if (onData(data, ex => continuation()) == TempEnum.CompletingAsynchronously)
            {
                return false;
            }
            return true;
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
