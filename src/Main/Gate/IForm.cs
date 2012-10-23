using System.Collections.Generic;

namespace Gate
{
    public interface IForm
    {
        IDictionary<string, string> Fields { get; }
        IDictionary<string, IFormFile> Files { get; }
    }
}
