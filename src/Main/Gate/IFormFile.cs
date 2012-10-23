using System.IO;

namespace Gate
{
    public interface IFormFile
    {
        string Name { get; }
        string FileName { get; }
        string ContentType { get; }
        long Size { get; }
        Stream Stream { get; }
    }
}
