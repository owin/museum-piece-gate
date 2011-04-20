using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using FakeItEasy;
using Gate.TestHelpers;
using NUnit.Framework;

namespace Gate.AspNet.Tests
{
    [TestFixture]
    public class AppHandlerTests
    {
        HttpContextBase _httpContext;
        HttpRequestBase _httpRequest;
        HttpResponseBase _httpResponse;
        MemoryStream _outputStream;

        [SetUp]
        public void Init()
        {
            _httpContext = A.Fake<HttpContextBase>();
            _httpRequest = A.Fake<HttpRequestBase>();
            _httpResponse = A.Fake<HttpResponseBase>();
            _outputStream = new MemoryStream();

            A.CallTo(() => _httpContext.Request).Returns(_httpRequest);
            A.CallTo(() => _httpContext.Response).Returns(_httpResponse);
            A.CallTo(() => _httpRequest.ServerVariables).Returns(new NameValueCollection());
            A.CallTo(() => _httpResponse.OutputStream).Returns(_outputStream);
        }

        string ResponseOutputText
        {
            get { return Encoding.UTF8.GetString(_outputStream.ToArray()); }
        }


        void SetRequestPaths(string url, string appPath)
        {
            var uri = new Uri(url);
            var path = "/" + uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped);
            
            A.CallTo(() => _httpRequest.Url).Returns(uri);
            A.CallTo(() => _httpRequest.Path).Returns(path);
            A.CallTo(() => _httpRequest.ApplicationPath).Returns(appPath);
            A.CallTo(() => _httpRequest.CurrentExecutionFilePath).Returns(path);
            A.CallTo(() => _httpRequest.AppRelativeCurrentExecutionFilePath).Returns("~" + path.Substring(appPath=="/"?0:appPath.Length));
        }

        void ProcessRequest(AppHandler appHandler)
        {
            var asyncResult = appHandler.BeginProcessRequest(_httpContext, null, null);
            appHandler.EndProcessRequest(asyncResult);
        }

        [Test]
        public void AppHandler_can_be_created_and_invoked()
        {
            SetRequestPaths("http://localhost/", "/");

            var app = new FakeApp("200 OK", "Hello World");
            var appHandler = new AppHandler(app.AppDelegate);

            ProcessRequest(appHandler);

            Assert.That(app.AppDelegateInvoked, Is.True);
            Assert.That(_httpResponse.Status, Is.EqualTo("200 OK"));
            Assert.That(ResponseOutputText, Is.EqualTo("Hello World"));
        }

        [Test]
        public void Uri_scheme_http_passes_through_to_environment()
        {
            SetRequestPaths("http://localhost/", "/");

            var app = new FakeApp("200 OK", "Hello World");
            var appHandler = new AppHandler(app.AppDelegate);

            ProcessRequest(appHandler);

            Assert.That(app.AppDelegateInvoked);
            Assert.That(app.Owin.Scheme, Is.EqualTo("http"));
        }

        [Test]
        public void Uri_scheme_https_passes_through_to_environment()
        {
            SetRequestPaths("https://localhost/", "/");

            var app = new FakeApp("200 OK", "Hello World");
            var appHandler = new AppHandler(app.AppDelegate);

            ProcessRequest(appHandler);

            Assert.That(app.AppDelegateInvoked);
            Assert.That(app.Owin.Scheme, Is.EqualTo("https"));
        }

        [Test]
        public void Path_and_pathbase_are_rooted()
        {
            SetRequestPaths("http://localhost/", "/");

            var app = new FakeApp("200 OK", "Hello World");
            var appHandler = new AppHandler(app.AppDelegate);

            ProcessRequest(appHandler);

            Assert.That(app.AppDelegateInvoked);
            Assert.That(app.Owin.Path, Is.EqualTo("/"));
            Assert.That(app.Owin.PathBase, Is.EqualTo(""));
        }

        [Test]
        public void Path_receives_path_portion_of_request()
        {
            SetRequestPaths("http://localhost/foo/bar", "/");

            var app = new FakeApp("200 OK", "Hello World");
            var appHandler = new AppHandler(app.AppDelegate);

            ProcessRequest(appHandler);

            Assert.That(app.AppDelegateInvoked);
            Assert.That(app.Owin.Path, Is.EqualTo("/foo/bar"));
            Assert.That(app.Owin.PathBase, Is.EqualTo(""));
        }

        [Test]
        public void PathBase_receives_leading_portion_when_apprelative_portion_is_shorter()
        {
            SetRequestPaths("http://localhost/foo/bar", "/foo");

            var app = new FakeApp("200 OK", "Hello World");
            var appHandler = new AppHandler(app.AppDelegate);

            ProcessRequest(appHandler);

            Assert.That(app.AppDelegateInvoked);
            Assert.That(app.Owin.Path, Is.EqualTo("/bar"));
            Assert.That(app.Owin.PathBase, Is.EqualTo("/foo"));
        }
    }
}