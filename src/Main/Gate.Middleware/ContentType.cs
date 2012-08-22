using Gate.Middleware;
using Owin;
using System.Threading.Tasks;

namespace Owin
{
    public static class ContentTypeExtensions
    {
        public static IAppBuilder UseContentType(this IAppBuilder builder)
        {
            return builder.UseType<ContentType>();
        }

        public static IAppBuilder UseContentType(this IAppBuilder builder, string contentType)
        {
            return builder.UseType<ContentType>(contentType);
        }
    }
}

namespace Gate.Middleware
{
    /// <summary>
    /// Sets content-type for the response if none is present.
    /// </summary>
    public class ContentType
    {
        readonly AppDelegate _next;
        readonly string _contentType;
        const string DefaultContentType = "text/html";

        public ContentType(AppDelegate next)
        {
            _next = next;
            _contentType = DefaultContentType;
        }

        public ContentType(AppDelegate next, string contentType)
        {
            _next = next;
            _contentType = contentType;
        }

        public Task<ResultParameters> Invoke(CallParameters call)
        {
            return _next(call).Then(result =>
            {
                if (!result.Headers.HasHeader("Content-Type"))
                {
                    result.Headers.SetHeader("Content-Type", _contentType);
                }
                return result;
            });
        }
    }
}
