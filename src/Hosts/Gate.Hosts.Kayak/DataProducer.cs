using System;
using System.Threading;
using Gate.Owin;
using Kayak;

namespace Gate.Hosts.Kayak
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
            var cts = new CancellationTokenSource();
            del(
                data => channel.OnData(data, null),
                _ => false,
                error =>
                {
                    if (error == null) 
                        channel.OnEnd(); 
                    else 
                        channel.OnError(error);
                },
                cts.Token);
            return new Disposable(cts.Cancel);
        }
    }
}
