using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.Middleware;
using System.Threading.Tasks;
using System.IO;
using Gate.Utils;

namespace Owin
{
    public static class XSendFileExtensions
    {
        public static IAppBuilder UseXSendFile(this IAppBuilder builder)
        {
            return builder.UseType<XSendFile>();
        }
    }
}

namespace Gate.Middleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using SendFileFunc = Func<string, long, long, Task>;

    // This middleware can be used to enable X-SendFile response header functionality.
    // Applications can set this header if they do not want to or otherwise can't 
    // efficiently serve the content of a file themselves.
    public class XSendFile
    {
        private readonly AppFunc nextApp;

        public XSendFile(AppFunc nextApp)
        {
            this.nextApp = nextApp;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            return this.nextApp(env).Then(() =>
            {
                Response response = new Response(env);
                string file = response.GetHeader("X-SendFile");
                if (string.IsNullOrWhiteSpace(file))
                {
                    return TaskHelpers.Completed();
                }

                response.Headers.Remove("X-SendFile");
                
                // TODO: Convert from a relative URL path to an absolute local file path
                FileInfo fileInfo = new FileInfo(file);
                if (!fileInfo.Exists)
                {
                    // Let the server send a 500 error
                    throw new FileNotFoundException();
                }

                // TODO: For now the application is required to set content-length or chunked, and content-range.

                SendFileFunc sendFile = env.Get<SendFileFunc>("sendfile.Func");
                if (sendFile != null)
                {
                    return sendFile(file, 0, fileInfo.Length);
                }

                // Fall back to a manual copy

                Stream responseBody = env.Get<Stream>(OwinConstants.ResponseBody);

                // Let the server send a 500 error
                Stream fileStream = fileInfo.OpenRead();
                try
                {
                    return fileStream.CopyToAsync(responseBody).Finally(() => fileStream.Close());
                }
                catch (Exception)
                {
                    fileStream.Close();
                    throw;
                }
            });
        }
    }
}
