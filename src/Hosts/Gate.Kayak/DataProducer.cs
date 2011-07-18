using System;
using Kayak;

namespace Gate.Kayak
{
    class DataProducer : IDataProducer
    {
        readonly BodyDelegate del;

        public DataProducer(BodyDelegate del)
        {   
            this.del = del;
        }

        public IDisposable Connect(IDataConsumer channel)
        {
            return new Disposable(del(
                (data, continuation) => channel.OnData(data, continuation),
                error => channel.OnError(error),
                () => channel.OnEnd()));
        }
    }
}
