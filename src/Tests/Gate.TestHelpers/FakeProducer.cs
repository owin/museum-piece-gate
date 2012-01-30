using Gate.Owin;

namespace Gate.TestHelpers
{
    using System;
    using System.Threading;
    //using BodyDelegate = System.Func<System.Func<System.ArraySegment<byte>, // data
    //    System.Action, // continuation
    //    bool>, // continuation will be invoked
    //    System.Action<System.Exception>, // onError
    //    System.Action, // on Complete
    //    System.Action>; // cancel

    public class FakeProducer
    {
        bool active;

        bool sendContinuation;
        int currentIndex;
        byte[] buffer;
        int chunkSize;
        bool autoSend;

        Func<ArraySegment<byte>, bool> _write;
        Func<Action, bool> _flush;
        Action<Exception> _end;
        CancellationToken _cancellationtoken;

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeProducer"/> class. 
        /// </summary>
        /// <param name="sendContinuation">Whether to send a continuation to the consumer.</param>
        /// <param name="buffer">Buffer to send.</param>
        /// <param name="chunkSize">Chunk size to split the buffer into.</param>
        /// <param name="autoSend">Automatically send all chunks when the body delegate is invoked.</param>
        public FakeProducer(bool sendContinuation, byte[] buffer, int chunkSize, bool autoSend)
        {
            this.sendContinuation = sendContinuation;
            this.buffer = buffer;
            this.chunkSize = chunkSize;
            this.autoSend = autoSend;
        }

        /// <summary>
        /// Gets a value indicating whether the body delegate has been invoked
        /// </summary>
        public bool BodyDelegateInvoked { get; private set; }

        /// <summary>
        /// Gets a value indicating whether all chunks have been sent
        /// </summary>
        public bool IsComplete
        {
            get { return this.currentIndex >= this.buffer.Length; }
        }

        /// <summary>
        /// Gets a value indicating whether the cancel delegate was called
        /// </summary>
        public bool Cancelled { get; private set; }

        /// <summary>
        /// The actual body delegate
        /// </summary>
        /// <param name="onNext">On next delegate</param>
        /// <param name="onError">On error delegate</param>
        /// <param name="onComplete">On complete delegate</param>
        /// <returns>Cancellation delegate</returns>
        public void BodyDelegate(Func<ArraySegment<byte>, bool> write, Func<Action, bool> flush, Action<Exception> end, CancellationToken cancellationtoken)
        {
            _write = write;
            _flush = flush;
            _end = end;
            _cancellationtoken = cancellationtoken;

            this.BodyDelegateInvoked = true;

            if (this.autoSend)
            {
                ThreadPool.QueueUserWorkItem((s) => this.SendAll());
            }
        }

        /// <summary>
        /// Throw an exception to the producer
        /// </summary>
        /// <param name="e">Exception to pass</param>
        public void ThrowException(Exception e)
        {
            if (!this.BodyDelegateInvoked)
            {
                throw new InvalidOperationException("Body delegate not yet invoked");
            }

            this._end.Invoke(e);
        }

        /// <summary>
        /// Send all chunks
        /// </summary>
        public void SendAll()
        {
            if (!this.BodyDelegateInvoked)
            {
                throw new InvalidOperationException("Body delegate not yet invoked");
            }

            while (!this.IsComplete)
            {
                this.SendChunk();
            }

            this._end.Invoke(null);
        }

        /// <summary>
        /// Send next chunk
        /// </summary>
        public void SendChunk()
        {
            if (!this.BodyDelegateInvoked)
            {
                throw new InvalidOperationException("Body delegate not yet invoked");
            }

            if (this.IsComplete)
            {
                return;
            }

            var remainingBytes = this.buffer.Length - this.currentIndex;
            var currentChunkSize = Math.Min(remainingBytes, this.chunkSize);

            var currentChunk = new ArraySegment<byte>(this.buffer, this.currentIndex, currentChunkSize);

            this.currentIndex += currentChunkSize;

            if (this.sendContinuation)
            {
                // The continuation sets the reset event. If the consumer
                // returns false, signifying it won't call the continuation,
                // we set it straight away.
                var sync = new ManualResetEventSlim();
                if (!this._write(currentChunk))
                {
                    // write false means transmit completed
                    sync.Set();
                }
                else if (!this._flush(sync.Set))
                {
                    // flush false means transmit completed, callback will not occur
                    sync.Set();
                }

                // Wait for the contination to be called, if it is going to be
                sync.Wait();
            }
            else
            {
                if (this._write(currentChunk) && this._flush(null))
                {
                    throw new InvalidOperationException("Consumer returned true for 'will invoke continuation' when continuation was null");
                }
            }
        }

        /// <summary>
        /// Invoke the onComplete delegate from the consumer
        /// </summary>
        public void InvokeOnComplete()
        {
            if (!this.BodyDelegateInvoked)
            {
                throw new InvalidOperationException("Body delegate not yet invoked");
            }

            this._end.Invoke(null);
        }

        void OnCancel()
        {
            this.Cancelled = true;

            this.active = false;
        }

        public static implicit operator BodyDelegate(FakeProducer producer)
        {
            return producer.BodyDelegate;
        }

    }
}