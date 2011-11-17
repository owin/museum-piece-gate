﻿using System.Collections.Generic;
using Gate.Builder;
using Gate.Helpers;
using Gate.Mapping;
using Gate.Owin;
using Gate.TestHelpers;
using NUnit.Framework;

namespace Gate.Tests
{
    [TestFixture]
    public class UrlMapperTests
    {
        [Test]
        public void Call_on_empty_map_defaults_to_status_404()
        {
            var map = new Dictionary<string, AppDelegate>();
            var app = UrlMapper.Create(map);
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("404 Not Found"));
            Assert.That(callResult.BodyText, Is.StringContaining("Not Found"));
        }

        [Test]
        public void Calling_mapped_path_hits_given_app()
        {
            var map = new Dictionary<string, AppDelegate>
            {
                {"/foo", Wilson.App()}
            };
            var app = UrlMapper.Create(map);

            var rootResult = AppUtils.Call(app);
            Assert.That(rootResult.Status, Is.EqualTo("404 Not Found"));
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
            Assert.That(rootResult.Status, Is.EqualTo("404 Not Found"));
            Assert.That(rootResult.BodyText, Is.StringContaining("Not Found"));

            var fooResult = AppUtils.Call(app, "/foo");
            Assert.That(fooResult.Status, Is.EqualTo("200 OK"));
            Assert.That(fooResult.BodyXml.Element(Environment.RequestPathBaseKey).Value, Is.EqualTo("/foo"));
            Assert.That(fooResult.BodyXml.Element(Environment.RequestPathKey).Value, Is.EqualTo(""));

            var fooBarResult = AppUtils.Call(app, "/foo/bar");
            Assert.That(fooBarResult.Status, Is.EqualTo("200 OK"));
            Assert.That(fooBarResult.BodyXml.Element(Environment.RequestPathBaseKey).Value, Is.EqualTo("/foo"));
            Assert.That(fooBarResult.BodyXml.Element(Environment.RequestPathKey).Value, Is.EqualTo("/bar"));
        }
    }
}