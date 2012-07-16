using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Owin;
using System.Threading.Tasks;

namespace Gate.Middleware
{
    public static partial class ShowExceptions
    {
        public static IAppBuilder UseShowExceptions(this IAppBuilder builder)
        {
            return builder.Use(Middleware);
        }

        public static AppDelegate Middleware(AppDelegate app)
        {
            return call =>
            {
                Action<Exception, Action<ArraySegment<byte>>> showErrorMessage =
                    (ex, write) =>
                        ErrorPage(call, ex, text =>
                        {
                            var data = Encoding.ASCII.GetBytes(text);
                            write(new ArraySegment<byte>(data));
                        });

                Func<Exception, Task<ResultParameters>> showErrorPage = ex =>
                {
                    var response = new Response() { Status = "500 Internal Server Error", ContentType = "text/html" };
                    showErrorMessage(ex, data => response.Write(data));
                    return response.EndAsync();
                };

                try
                {
                    return app(call).ContinueWith<ResultParameters>(
                        appTask =>
                        {
                            if (!appTask.IsFaulted && !appTask.IsCanceled)
                            {
                                ResultParameters result = appTask.Result;
                                if (result.Body != null)
                                {
                                    BodyDelegate nestedBody = result.Body;
                                    result.Body = (stream, cancel) =>
                                        {
                                            // TODO: Handle sync and async errors from the nested body.
                                            return nestedBody(stream, cancel);
                                        };
                                }
                                return result;
                            }

                            if (appTask.Exception != null)
                            {
                                return showErrorPage(appTask.Exception).Result;
                            }
                            else
                            {
                                return showErrorPage(new TaskCanceledException()).Result;
                            }
                        });
                }
                catch (Exception exception)
                {
                    return showErrorPage(exception);
                }
            };
        }

        static string h(object text)
        {
            return Convert.ToString(text).Replace("<", "&lt;").Replace(">", "&gt;");
        }


        static IEnumerable<Frame> StackFrames(Exception ex)
        {
            return StackFrames(StackTraces(ex).Reverse());
        }

        static IEnumerable<string> StackTraces(Exception ex)
        {
            for (var scan = ex; scan != null; scan = scan.InnerException)
            {
                yield return ex.StackTrace;
            }
        }

        static IEnumerable<Frame> StackFrames(IEnumerable<string> stackTraces)
        {
            foreach (var stackTrace in stackTraces.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                var heap = new Chunk { Text = stackTrace + "\r\n", End = stackTrace.Length + 2 };
                for (var line = heap.Advance("\r\n"); line.HasValue; line = heap.Advance("\r\n"))
                {
                    yield return StackFrame(line);
                }
            }
        }

        static Frame StackFrame(Chunk line)
        {
            line.Advance("  at ");
            var function = line.Advance(" in ").ToString();
            var file = line.Advance(":line ").ToString();
            var lineNumber = line.ToInt32();

            return string.IsNullOrEmpty(file)
                ? LoadFrame(line.ToString(), "", 0)
                : LoadFrame(function, file, lineNumber);
            ;
        }

        static Frame LoadFrame(string function, string file, int lineNumber)
        {
            var frame = new Frame { Function = function, File = file, Line = lineNumber };
            if (File.Exists(file))
            {
                var code = File.ReadAllLines(file);
                frame.PreContextLine = Math.Max(lineNumber - 6, 1);
                frame.PreContextCode = code.Skip(frame.PreContextLine - 1).Take(lineNumber - frame.PreContextLine).ToArray();
                frame.ContextCode = code.Skip(lineNumber - 1).FirstOrDefault();
                frame.PostContextCode = code.Skip(lineNumber).Take(6).ToArray();
            }
            return frame;
        }

        internal class Chunk
        {
            public string Text;
            public int Start;
            public int End;

            public bool HasValue
            {
                get { return Text != null; }
            }

            public Chunk Advance(string delimiter)
            {
                var indexOf = HasValue ? Text.IndexOf(delimiter, Start, End - Start) : -1;
                if (indexOf < 0)
                    return new Chunk();

                var chunk = new Chunk { Text = Text, Start = Start, End = indexOf };
                Start = indexOf + delimiter.Length;
                return chunk;
            }

            public override string ToString()
            {
                return HasValue ? Text.Substring(Start, End - Start) : "";
            }

            public int ToInt32()
            {
                int value;
                return HasValue && Int32.TryParse(
                    Text.Substring(Start, End - Start),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out value) ? value : 0;
            }
        }

        public class Frame
        {
            public string Function;
            public string File;
            public int Line;

            public int PreContextLine;
            public string[] PreContextCode;
            public string ContextCode;
            public string[] PostContextCode;
        }
    }
}