using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gate.Utils
{
    // #if NET40
    public static class Net40Extensions
    {
        public static Task WriteAsync(this Stream stream, byte[] buffer, int offset, int count)
        {
            var tcs = new TaskCompletionSource<object>();
            var sr = stream.BeginWrite(buffer, offset, count, ar =>
            {
                if (ar.CompletedSynchronously)
                {
                    return;
                }
                try
                {
                    stream.EndWrite(ar);
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);

            if (sr.CompletedSynchronously)
            {
                try
                {
                    stream.EndWrite(sr);
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }
            return tcs.Task;
        }

        public static Task FlushAsync(this Stream stream)
        {
            stream.Flush();
            return TaskHelpers.Completed();
        }
    }
    // #endif
}
