using System.Collections.Generic;
using Gate.Mapping;
using Owin;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Gate.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    [TestFixture]
    public class UrlMapperTests
    {
        AppFunc NotFound = call => { call.Set("owin.ResponseStatusCode", 404); return TaskHelpers.Completed(); };

        private IDictionary<string, object> CreateEmptyEnvironment()
        {
            return new Dictionary<string, object>()
            {
                { "owin.RequestHeaders",  Headers.New() },
                { "owin.ResponseHeaders", Headers.New() },
            };
        }

        [Test]
        public void Call_on_empty_map_defaults_to_status_404()
        {
            var map = new Dictionary<string, AppFunc>();
            var app = UrlMapper.Create(NotFound, map);
            var env = CreateEmptyEnvironment();
            app(env).Wait();
            Assert.That(env.Get<int>("owin.ResponseStatusCode"), Is.EqualTo(404));
        }

        //[Test]
        //public void Calling_mapped_path_hits_given_app()
        //{
        //    var map = new Dictionary<string, AppFunc>
        //    {
        //        {"/foo", Wilson.App()}
        //    };
        //    var app = UrlMapper.Create(map);

        //    var rootResult = AppUtils.Call(app);
        //    Assert.That(rootResult.Status, Is.EqualTo("404 Not Found"));
        //    Assert.That(rootResult.BodyText, Is.StringContaining("Not Found"));

        //    var fooResult = AppUtils.Call(app, "/foo");
        //    Assert.That(fooResult.Status, Is.EqualTo("200 OK"));
        //    Assert.That(fooResult.BodyText, Is.StringContaining("Wilson"));
        //}
        /*
        [Test]
        public void Path_and_PathBase_are_adjusted_by_location()
        {
            var map = new Dictionary<string, AppFunc>
            {
                {"/foo", AppUtils.ShowEnvironment()}
            };

            var app = UrlMapper.Create(NotFound, map);

            var rootResult = app(new CallParameters()).Result;
            Assert.That(rootResult.Status, Is.EqualTo("404 Not Found"));

            var fooResult = AppUtils.Call(app, "/foo");
            Assert.That(fooResult.Status, Is.EqualTo("200 OK"));
            Assert.That(fooResult.BodyXml.Element(Environment.RequestPathBaseKey).Value, Is.EqualTo("/foo"));
            Assert.That(fooResult.BodyXml.Element(Environment.RequestPathKey).Value, Is.EqualTo(""));

            var fooBarResult = AppUtils.Call(app, "/foo/bar");
            Assert.That(fooBarResult.Status, Is.EqualTo("200 OK"));
            Assert.That(fooBarResult.BodyXml.Element(Environment.RequestPathBaseKey).Value, Is.EqualTo("/foo"));
            Assert.That(fooBarResult.BodyXml.Element(Environment.RequestPathKey).Value, Is.EqualTo("/bar"));
        }*/
    }
}