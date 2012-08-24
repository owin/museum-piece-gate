﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using Owin;
using System.Threading;

namespace Gate.Tests
{
    // ReSharper disable InconsistentNaming
    [TestFixture]
    public class RequestTests
    {
        private IDictionary<string, object> CreateEmptyEnvironment()
        {
            return new Dictionary<string, object>()
            {
                { "owin.RequestHeaders",  Headers.New() },
                { "owin.ResponseHeaders", Headers.New() },
            };
        }

        [Test]
        public void QueryString_is_used_to_populate_Query_dictionary()
        {
            var request = new Request(CreateEmptyEnvironment()) { QueryString = "foo=bar" };
            Assert.That(request.Query["foo"], Is.EqualTo("bar"));
        }

        [Test]
        public void Changing_QueryString_in_environment_reparses_Query_dictionary()
        {
            var request = new Request(CreateEmptyEnvironment()) { QueryString = "foo=bar" };
            Assert.That(request.Query["foo"], Is.EqualTo("bar"));

            request.QueryString = "foo=quux";
            Assert.That(request.Query["foo"], Is.EqualTo("quux"));
        }

        //[Test]
        //public void Body_is_used_to_populate_Post_dictionary()
        //{
        //    var request = new Request(CreateEmptyCall()) { Method = "POST", BodyDelegate = Body.FromText("foo=bar") };
        //    Assert.That(request.Post["foo"], Is.EqualTo("bar"));
        //}

        //[Test]
        //public void Changing_Body_in_environment_reparses_Post_dictionary()
        //{
        //    var request = new Request(CreateEmptyCall()) { Method = "POST", BodyAction = Body.FromText("foo=bar") };
        //    Assert.That(request.Post["foo"], Is.EqualTo("bar"));

        //    request.BodyAction = Body.FromText("foo=quux");
        //    Assert.That(request.Post["foo"], Is.EqualTo("quux"));
        //}

        [Test]
        public void Host_will_remove_port_from_request_header_if_needed()
        {
            var env = CreateEmptyEnvironment();
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").SetHeader("Host", "Beta:8080");
            var request = new Request(env);
            Assert.That(request.Host, Is.EqualTo("Beta"));
        }

        [Test]
        public void Host_is_empty_if_nothing_provided()
        {
            var env = CreateEmptyEnvironment();
            var request = new Request(env);
            Assert.That(request.Host, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Port_will_remove_port_from_request_header_if_needed()
        {
            var env = CreateEmptyEnvironment();
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").SetHeader("Host", "Beta:8080");
            var request = new Request(env);
            Assert.That(request.Port, Is.EqualTo("8080"));
        }

        [Test]
        public void Port_is_empty_if_nothing_provided()
        {
            var env = CreateEmptyEnvironment();
            var request = new Request(env);
            Assert.That(request.Port, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Port_is_empty_if_only_host_provided()
        {
            var env = CreateEmptyEnvironment();
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").SetHeader("Host", "Beta");
            var request = new Request(env);
            Assert.That(request.Port, Is.EqualTo(string.Empty));
        }

        [Test]
        public void ContentType_and_MediaType_should_return_http_header()
        {
            var env = CreateEmptyEnvironment();
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").SetHeader("Content-Type", "text/plain");
            var request = new Request(env);
            Assert.That(request.ContentType, Is.EqualTo("text/plain"));
            Assert.That(request.MediaType, Is.EqualTo("text/plain"));
        }

        [Test]
        public void MediaType_is_shorter_when_delimited()
        {
            var env = CreateEmptyEnvironment();
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").SetHeader("Content-Type", "text/html; charset=utf-8");
            var request = new Request(env);
            Assert.That(request.ContentType, Is.EqualTo("text/html; charset=utf-8"));
            Assert.That(request.MediaType, Is.EqualTo("text/html"));
        }

        [Test]
        public void ContentType_and_MediaType_are_null_when_missing()
        {
            var env = CreateEmptyEnvironment();
            var request = new Request(env);
            Assert.That(request.ContentType, Is.Null);
            Assert.That(request.MediaType, Is.Null);
        }

        [Test]
        public void ItShouldParseCookies()
        {
            var env = CreateEmptyEnvironment();
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").SetHeader("Cookie", "foo=bar;quux=h&m");
            var request = new Request(env);
            Assert.That(request.Cookies["foo"], Is.EqualTo("bar"));
            Assert.That(request.Cookies["quux"], Is.EqualTo("h&m"));
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").Remove("Cookie");
            Assert.That(request.Cookies.Count, Is.EqualTo(0));
        }

        [Test]
        public void ItShouldAlwaysReturnTheSameDictionaryObject()
        {
            var env = CreateEmptyEnvironment();
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").SetHeader("Cookie", "foo=bar;quux=h&m");
            var request = new Request(env);
            var cookies = request.Cookies;
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").Remove("Cookie");
            Assert.That(request.Cookies, Is.SameAs(cookies));
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").SetHeader("Cookie", "zoo=m");
            Assert.That(request.Cookies, Is.SameAs(cookies));
        }

        [Test]
        public void ItShouldModifyTheCookiesDictionaryInPlace()
        {
            var env = CreateEmptyEnvironment();
            var request = new Request(env);
            var cookies = request.Cookies;
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").Remove("Cookie");
            Assert.That(cookies.Count, Is.EqualTo(0));
            cookies["foo"] = "bar";
            Assert.That(cookies.Count, Is.EqualTo(1));
            Assert.That(request.Cookies["foo"], Is.EqualTo("bar"));
        }


        [Test]
        public void ItShouldParseCookiesAccordingToRFC2109()
        {
            var env = CreateEmptyEnvironment();
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").SetHeader("Cookie", "foo=bar;foo=car");
            var request = new Request(env);
            Assert.That(request.Cookies["foo"], Is.EqualTo("bar"));
        }

        [Test]
        public void ItShouldParseCookiesWithQuotes()
        {
            var env = CreateEmptyEnvironment();
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders")
                .SetHeader("Cookie", @"$Version=""1""; Customer=""WILE_E_COYOTE""; $Path=""/acme""; Part_Number=""Rocket_Launcher_0001""; $Path=""/acme""");
            var request = new Request(env);

            Assert.That(request.Cookies["$Version"], Is.EqualTo(@"""1"""));
            Assert.That(request.Cookies["Customer"], Is.EqualTo(@"""WILE_E_COYOTE"""));
            Assert.That(request.Cookies["$Path"], Is.EqualTo(@"""/acme"""));
            Assert.That(request.Cookies["Part_Number"], Is.EqualTo(@"""Rocket_Launcher_0001"""));
        }

    }
}