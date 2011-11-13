using System;
using System.Text;
using Gate.Owin;

namespace Gate.Middleware
{
    public class TextBody
    {
        private readonly string text;
        private readonly Encoding encoding;
        private BodyStream bodyStream;

        public TextBody(string text, Encoding encoding)
        {
            this.text = text;
            this.encoding = encoding;
        }

        public static BodyDelegate Create(string text, Encoding encoding)
        {
            return (data, error, complete) =>
            {
                var textBody = new TextBody(text, encoding);

                return textBody.Start(data, error, complete);
            };
        }

        public Action Start(Func<ArraySegment<byte>, Action, bool> data, Action<Exception> error, Action complete)
        {
            bodyStream = new BodyStream(data, error, complete);

            Action start = () =>
            {
                try
                {
                    if (bodyStream.CanSend())
                    {
                        var bytes = encoding.GetBytes(text);
                        var segment = new ArraySegment<byte>(bytes);

                        // Not buffered.
                        bodyStream.SendBytes(segment, null, null);

                        bodyStream.Finish();
                    }
                }
                catch (Exception ex)
                {
                    bodyStream.Error(ex);
                }
            };

            bodyStream.Start(start, null);

            return bodyStream.Cancel;
        }
    }
}