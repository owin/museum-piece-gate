using System.Collections.Generic;
using Gate.Mapping;
using Owin;
using Gate.TestHelpers;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Gate.Tests
{
    [TestFixture]
    public class UrlMapperTests
    {
        AppDelegate NotFound = call => TaskHelpers.FromResult(new ResultParameters() { Status = 404 });

        private CallParameters CreateEmptyCall()
        {
            return new CallParameters()
            {
                Body = null,
                Environment = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase),
                Headers = Headers.New(),
            };
        }

        [Test]
        public void Call_on_empty_map_defaults_to_status_404()
        {
            var map = new Dictionary<string, AppDelegate>();
            var app = UrlMapper.Create(NotFound, map);
            var callResult = app(CreateEmptyCall()).Result;
            Assert.That(callResult.Status, Is.EqualTo(404));
        }

        //[Test]
        //public void Calling_mapped_path_hits_given_app()
        //{
        //    var map = new Dictionary<string, AppDelegate>
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
            var map = new Dictionary<string, AppDelegate>
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