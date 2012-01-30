using Gate.Owin;

namespace Gate.TestHelpers
{
    using System;
    using System.IO;
    using System.Threading;
    using Gate;

    /// <summary>
    /// Consumes a body delegate
    /// </summary>
    public class FakeConsumer
    {
        readonly bool useContinuation;

        Action cancelDelegate;

        bool bodyDelegateInvoked;

        MemoryStream dataStream;
        ManualResetEventSlim sync = new ManualResetEventSlim();

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeConsumer"/> class.
        /// </summary>
        /// <param name="useContinuation">Whether to use async/the continuation if supplied.</param>
        public FakeConsumer(bool useContinuation)
        {
            this.useContinuation = useContinuation;
        }

        /// <summary>
        /// Gets a value indicating whether complete has been called.
        /// </summary>
        public bool CompleteCalled { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a non-null continuation was sent by the producer.
        /// </summary>
        public bool ContinuationSent { get; private set; }

        /// <summary>
        /// Gets the data that was consumed - only valid after complete called.
        /// </summary>
        public byte[] ConsumedData { get; private set; }

        /// <summary>
        /// Gets the exeption that was thrown - only set if the error delegate was invoked
        /// </summary>
        public Exception RaisedException { get; private set; }

        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Invoke the body delegate
        /// </summary>
        /// <param name="bodyDelegate">The body delegate to invoke</param>
        public void InvokeBodyDelegate(BodyDelegate bodyDelegate, bool waitForComplete = true)
        {
            if (bodyDelegate == null)
            {
                throw new ArgumentNullException("bodyDelegate");
            }

            this.sync.Reset();

            this.dataStream = new MemoryStream();

            if (waitForComplete)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    bodyDelegate.Invoke(this.OnWrite, this.OnFlush, this.OnEnd, this.CancellationToken);
                    this.bodyDelegateInvoked = true;
                });
                this.sync.Wait();
            }
            else
            {
                bodyDelegate.Invoke(this.OnWrite, this.OnFlush, this.OnEnd, this.CancellationToken);
                this.bodyDelegateInvoked = true;
            }
        }




        /// <summary>
        /// Invokes the cancel delegate returned from the body delegate
        /// </summary>
        public void InvokeCancel()
        {
            if (!this.bodyDelegateInvoked)
            {
                throw new InvalidOperationException("Body delegate must be invoked before it can be cancelled!");
            }

            this.cancelDelegate.Invoke();

            this.sync.Set();
        }

        void OnEnd(Exception exception)
        {
            if (exception == null)
            {
                this.CompleteCalled = true;
                this.dataStream.Close();
                this.ConsumedData = this.dataStream.ToArray();
            }
            else
            {
                this.RaisedException = exception;
                this.dataStream.Dispose();
            }
            this.sync.Set();
        }

        bool OnWrite(ArraySegment<byte> data)
        {
            // No continuation - consume sync.
            // and return false to indicate we won't be calling the continuation
            this.ConsumeDataSync(data);
            return false;
        }

        bool OnFlush(Action continuation)
        {
            this.ContinuationSent = continuation != null;

            return false;
        }

        void ConsumeDataSync(ArraySegment<byte> data)
        {
            this.dataStream.Write(data.Array, data.Offset, data.Count);
        }

    }
}