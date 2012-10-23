using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Gate.Utils;

namespace Gate.Form
{
    internal class Form : IForm
    {
        readonly Encoding _encoding = new ASCIIEncoding();

        public Form()
        {
            Fields = ParamDictionary.Parse("");
            Files = new Dictionary<string, IFormFile>();
        }

        public Form(string text)
        {
            Fields = ParamDictionary.Parse(text);
            Files = new Dictionary<string, IFormFile>();
        }

        public Form(string boundary, Stream stream)
        {
            Fields = new Dictionary<string, string>();
            Files = new Dictionary<string, IFormFile>();
            if (stream == null)
                return;
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
            ReadData(boundary, stream);
        }

        private static int IndexOf(byte[] searchIn, byte[] searchBytes, int start = 0)
        {
            var found = -1;
            if (searchIn.Length > 0 && searchBytes.Length > 0 && start <= (searchIn.Length - searchBytes.Length) && searchIn.Length >= searchBytes.Length)
            {
                for (var i = start; i <= searchIn.Length - searchBytes.Length; i++)
                {
                    if (searchIn[i] != searchBytes[0]) continue;
                    if (searchIn.Length > 1)
                    {
                        var matched = true;
                        for (var y = 1; y <= searchBytes.Length - 1; y++)
                        {
                            if (searchIn[i + y] == searchBytes[y]) continue;
                            matched = false;
                            break;
                        }
                        if (matched)
                        {
                            found = i;
                            break;
                        }
                    }
                    else
                    {
                        found = i;
                        break;
                    }
                }
            }
            return found;
        }

        private static byte[] ReadFully(Stream input)
        {
            var buffer = new byte[16 * 1024];
            using (var ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        private static byte[] SubArray(byte[] data, int index, int length)
        {
            var result = new byte[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        private void ReadData(string boundary, Stream input)
        {
            var data = ReadFully(input);

            var b = _encoding.GetBytes(boundary);
            var ret1 = _encoding.GetBytes("\r\n--"+boundary);
            var ret2 = _encoding.GetBytes("\r\n\r\n");

            var offset = 0;
            int pos;
            while ((pos = IndexOf(data, b, offset)) != -1)
            {
                // pos - boundary starting point
                if (pos + b.Length + 2 - data.Length > -5)
                    return;
                // pos2 - boundary header endpoint/boundary body start point
                var pos2 = IndexOf(data, ret2, pos + 1);
                if (pos2 == -1)
                    return;
                pos2 += ret2.Length;
                // pos3 - boundary endpoint
                var pos3 = IndexOf(data, ret1, pos2);
                if (pos3 == -1)
                    return;
                var file = new FormFile(SubArray(data, pos, pos2 - pos), SubArray(data, pos2, pos3 - pos2), _encoding);
                if (file.IsFile)
                    Files[file.Name] = file;
                else
                    Fields[file.Name] = file.FileName;
                offset = pos3;
            }
        }

        public IDictionary<string, string> Fields { get; private set; }

        public IDictionary<string, IFormFile> Files { get; private set; }
    }
}
