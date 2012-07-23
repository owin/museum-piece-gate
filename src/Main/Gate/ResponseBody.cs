namespace Gate
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Gate.Utils;
    using Owin;

    /// <summary>
    /// This is a helper class for the BodyDelegate, for use with the Response class or ResultParameter struct.
    /// This can be used for either buffering or streaming, depending on the constructor.
    /// </summary>
    public class ResponseBody
    {
        private static readonly Encoding defaultEncoding = Encoding.UTF8;

        private BodyDelegate bodyDelegate;
        private Stream destinationStream;

        // For live streaming only
        private TaskCompletionSource<object> streamingCompletion;

        // Buffer
        public ResponseBody()
        {
            Encoding = defaultEncoding;
            destinationStream = new MemoryStream();
            bodyDelegate = CopyBufferToOutput;
        }
        
        // Buffer
        public ResponseBody(string bodyText)
            : this(bodyText, defaultEncoding)
        {
        }
        
        // Buffer
        public ResponseBody(string bodyText, Encoding encoding)
        {
            Encoding = encoding ?? defaultEncoding;
            destinationStream = new MemoryStream(Encoding.GetBytes(bodyText));
            bodyDelegate = CopyBufferToOutput;
        }
        
        // Buffer
        public ResponseBody(byte[] bodyBytes)
        {
            Encoding = defaultEncoding;
            destinationStream = new MemoryStream(bodyBytes);
            bodyDelegate = CopyBufferToOutput;
        }

        // Streaming callback
        public ResponseBody(Func<ResponseBody, Task> bodyCallback)
        {
            if (bodyCallback == null)
            {
                throw new ArgumentNullException("bodyCallback");
            }

            Encoding = defaultEncoding;
            streamingCompletion = new TaskCompletionSource<object>();
            
            bodyDelegate = (stream, cancel) =>
            {
                CancelToken = cancel;
                destinationStream = stream;
                return bodyCallback(this);
            };
        }

        // Streaming, for use directly within a BodyDelegate callback.
        // The Delegate property will not be available.
        public ResponseBody(Stream outputStream, CancellationToken cancel)
        {
            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            Encoding = defaultEncoding;
            destinationStream = outputStream;
            CancelToken = cancel;
            streamingCompletion = new TaskCompletionSource<object>();
        }

        public bool IsBuffered 
        { 
            get 
            { 
                return bodyDelegate == CopyBufferToOutput; 
            } 
        }

        public long BufferedLength
        {
            get
            {
                if (!IsBuffered)
                {
                    throw new InvalidOperationException("Not buffered");
                }

                return OutputStream.Length;
            }
        }

        public BodyDelegate Delegate
        { 
            get 
            {
                if (bodyDelegate == null)
                {
                    throw new InvalidOperationException("The delegate is not available for this constructor.");
                }

                return bodyDelegate; 
            } 
        }

        // Buffered or streaming
        public Stream OutputStream
        {
            get
            {
                if (destinationStream == null)
                {
                    throw new InvalidOperationException("Output stream not set.");
                }

                return destinationStream;
            }
        }

        public Encoding Encoding { get; set; }

        public CancellationToken CancelToken { get; set; }

        private Task CopyBufferToOutput(Stream output, CancellationToken cancel)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            destinationStream.Seek(0, SeekOrigin.Begin);
            return destinationStream.CopyToAsync(output, cancel);
        }

        public void Write(string text)
        {
            var data = (Encoding ?? defaultEncoding).GetBytes(text);
            Write(data);
        }

        public void Write(string format, params object[] args)
        {
            Write(string.Format(format, args));
        }
        
        public void Write(byte[] buffer)
        {
            CancelToken.ThrowIfCancellationRequested();
            OutputStream.Write(buffer, 0, buffer.Length);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            CancelToken.ThrowIfCancellationRequested();
            OutputStream.Write(buffer, offset, count);
        }

        public void Write(ArraySegment<byte> data)
        {
            CancelToken.ThrowIfCancellationRequested();
            OutputStream.Write(data.Array, data.Offset, data.Count);
        }

        public Task WriteAsync(byte[] buffer, int offset, int count)
        {
            return OutputStream.WriteAsync(buffer, offset, count, CancelToken);
        }

        public Task WriteAsync(ArraySegment<byte> data)
        {
            return OutputStream.WriteAsync(data.Array, data.Offset, data.Count, CancelToken);
        }

        public IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<object>(state);
            OutputStream.WriteAsync(buffer, offset, count, CancelToken).CopyResultToCompletionSource(tcs, null);
            return tcs.Task;
        }

        public void EndWrite(IAsyncResult result)
        {
            ((Task)result).Wait();
        }

        public void Flush()
        {
            OutputStream.Flush();
        }

        public Task FlushAsync()
        {
            return OutputStream.FlushAsync();
        }

        public Task StreamingTask
        {
            get
            {
                if (streamingCompletion == null)
                {
                    throw new InvalidOperationException("Not streaming");
                }

                return streamingCompletion.Task;
            }
        }

        public void EndBody()
        {
            EndBodyAsync();
        }

        public Task EndBodyAsync()
        {
            if (streamingCompletion != null)
            {
                streamingCompletion.TrySetResult(null);
                return streamingCompletion.Task;
            }
            return TaskHelpers.Completed();
        }

        public void FailBody(Exception ex)
        {
            FailBodyAsync(ex);
        }

        public Task FailBodyAsync(Exception ex)
        {
            if (streamingCompletion != null)
            {
                streamingCompletion.TrySetException(ex);
                return streamingCompletion.Task;
            }
            return TaskHelpers.FromError(ex);
        }
    }
}
