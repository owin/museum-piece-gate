using System;
using System.Collections.Generic;
using Gate.Helpers;
using Gate.TestHelpers;
using NUnit.Framework;

namespace Gate.Tests.Helpers
{
    using AppDelegate = Action< // app
        IDictionary<string, object>, // env
        Action< // result
            string, // status
            IDictionary<string, string>, // headers
            Func< // body
                Func< // next
                    ArraySegment<byte>, // data
                    Action, // continuation
                    bool>, // async                    
                Action<Exception>, // error
                Action, // complete
                Action>>, // cancel
        Action<Exception>>; // error

    [TestFixture]
    public class UrlMapperTests
    {
        [Test]
        public void Call_on_empty_map_defaults_to_status_404()
        {
            var map = new Dictionary<string, AppDelegate>();
            var app = UrlMapper.Create(map);
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("404 NotFound"));
            Assert.That(callResult.BodyText, Is.StringContaining("Not Found"));
        }

        [Test]
        public void Calling_mapped_path_hits_given_app()
        {
            var map = new Dictionary<string, AppDelegate>
            {
                {"/foo", Wilson.Create()}
            };
            var app = UrlMapper.Create(map);

            var rootResult = AppUtils.Call(app);
            Assert.That(rootResult.Status, Is.EqualTo("404 NotFound"));
            Assert.That(rootResult.BodyText, Is.StringContaining("Not Found"));

            var fooResult = AppUtils.Call(app, "/foo");
            Assert.That(fooResult.Status, Is.EqualTo("200 OK"));
            Assert.That(fooResult.BodyText, Is.StringContaining("Wilson"));
        }

        [Test]
        public void Path_and_PathBase_are_adjusted_by_location()
        {
            var map = new Dictionary<string, AppDelegate>
            {
                {"/foo", AppUtils.ShowEnvironment()}
            };

            var app = UrlMapper.Create(map);

            var rootResult = AppUtils.Call(app);
            Assert.That(rootResult.Status, Is.EqualTo("404 NotFound"));
            Assert.That(rootResult.BodyText, Is.StringContaining("Not Found"));

            var fooResult = AppUtils.Call(app, "/foo");
            Assert.That(fooResult.Status, Is.EqualTo("200 OK"));
            Assert.That(fooResult.BodyXml.Element(Owin.RequestPathBaseKey).Value, Is.EqualTo("/foo"));
            Assert.That(fooResult.BodyXml.Element(Owin.RequestPathKey).Value, Is.EqualTo(""));

            var fooBarResult = AppUtils.Call(app, "/foo/bar");
            Assert.That(fooBarResult.Status, Is.EqualTo("200 OK"));
            Assert.That(fooBarResult.BodyXml.Element(Owin.RequestPathBaseKey).Value, Is.EqualTo("/foo"));
            Assert.That(fooBarResult.BodyXml.Element(Owin.RequestPathKey).Value, Is.EqualTo("/bar"));

        }
    }
}