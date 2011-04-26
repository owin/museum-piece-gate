using System;
using System.IO;
using System.Text;

namespace Gate.Utils
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
    }
}