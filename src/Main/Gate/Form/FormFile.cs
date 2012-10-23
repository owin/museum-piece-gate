using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Gate.Form
{
    internal class FormFile : IFormFile
    {
        readonly Stream _stream;

        public FormFile(byte[] infob, byte[] content, Encoding encoding)
        {
            var info = encoding.GetString(infob);
            ContentType = null;
            Size = -1;
            var parts = info.Split(new[] {";", "\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts.Select(x => x.Trim()))
            {
                if (part.StartsWith("name=", StringComparison.OrdinalIgnoreCase))
                {
                    Name = UnEscape(part.Substring(5));
                }
                else if (part.StartsWith("filename=", StringComparison.OrdinalIgnoreCase))
                {
                    FileName = UnEscape(part.Substring(9));
                    Size = content.Length;
                }
                else if (part.StartsWith("Content-Type: ", StringComparison.OrdinalIgnoreCase))
                {
                    ContentType = UnEscape(part.Substring(14));
                }
            }

            _stream = ContentType != null ? new MemoryStream(content) : null;
            if (ContentType == null)
                FileName = encoding.GetString(content);
        }

        private static string UnEscape(string v)
        {
            return v.Trim(' ', '"');
        }

        public bool IsFile { get { return Stream != null; } }

        public string Name { get; private set; }

        public string FileName { get; private set; }

        public string ContentType { get; private set; }

        public long Size { get; private set; }

        public Stream Stream
        {
            get { return _stream; }
        }
    }
}
