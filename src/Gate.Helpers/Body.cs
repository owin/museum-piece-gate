using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Gate.Helpers
{
    using BodyAction = Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action>;

    public static class Body
    {
        public static BodyAction FromText(string text)
        {
            return FromText(text, Encoding.UTF8);
        }

        public static BodyAction FromText(string text, Encoding encoding)
        {
            return (data, error, complete) =>
            {
                if (!data(new ArraySegment<byte>(encoding.GetBytes(text)), complete))
                    complete();

                return () => { };
            };
        }


        public static string ToText(this BodyAction body, Encoding encoding)
        {
            var sb = new StringBuilder();
            var wait = new ManualResetEvent(false);
            Exception exception = null;
            body.Invoke(
                (data, _) =>
                {
                    sb.Append(encoding.GetString(data.Array, data.Offset, data.Count));
                    return false;
                },
                ex =>
                {
                    exception = ex;
                    wait.Set();
                },
                () => wait.Set());

            wait.WaitOne();
            if (exception != null)
                throw new AggregateException(exception);
            return sb.ToString();
        }

        public static BodyAction FromStream(Stream stream)
        {
            return (next, error, complete) =>
            {
                var buffer = new byte[4096];
                var continuation = new AsyncCallback[1];
                bool[] stopped = {false};
                continuation[0] = result =>
                {
                    if (result != null && result.CompletedSynchronously) return;
                    try
                    {
                        for (;;)
                        {
                            if (result != null)
                            {
                                var count = stream.EndRead(result);
                                if (stopped[0]) return;
                                if (count <= 0)
                                {
                                    complete();
                                    return;
                                }
                                var data = new ArraySegment<byte>(buffer, 0, count);
                                if (next(data, () => continuation[0](null))) return;
                            }

                            if (stopped[0]) return;
                            result = stream.BeginRead(buffer, 0, buffer.Length, continuation[0], null);
                            if (!result.CompletedSynchronously) return;
                        }
                    }
                    catch (Exception ex)
                    {
                        error(ex);
                    }
                };
                continuation[0](null);
                return () => { stopped[0] = true; };
            };
        }

        public static Action WriteToStream(this BodyDelegate body, Stream stream, Action<Exception> error, Action complete)
        {
            return body.ToAction().WriteToStream(stream, error, complete);
        }

        public static Action WriteToStream(this BodyAction body, Stream stream, Action<Exception> error, Action complete)
        {
            Action[] cancel = {() => { }};
            int[] completion = {0};
            Action<Exception> errorHandler = ex => { if (Interlocked.Increment(ref completion[0]) == 1) error(ex); };
            cancel[0] = body(
                (data, continuation) =>
                {
                    if (completion[0] != 0)
                        return false;
                    try
                    {
                        if (continuation == null)
                        {
                            stream.Write(data.Array, data.Offset, data.Count);
                            return false;
                        }
                        var sr = stream.BeginWrite(data.Array, data.Offset, data.Count, ar =>
                        {
                            if (ar.CompletedSynchronously) return;
                            try
                            {
                                stream.EndWrite(ar);
                            }
                            catch (Exception ex)
                            {
                                error(ex);
                            }
                            continuation();
                        }, null);
                        if (sr.CompletedSynchronously)
                        {
                            stream.EndWrite(sr);
                            return false;
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
                        errorHandler(ex);
                        cancel[0]();
                        return false;
                    }
                },
                errorHandler,
                () => { if (Interlocked.Increment(ref completion[0]) == 1) complete(); });

            return () =>
            {
                Interlocked.Increment(ref completion[0]);
                cancel[0]();
            };
        }
    }
}