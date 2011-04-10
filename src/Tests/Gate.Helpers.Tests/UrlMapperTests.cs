using System;
using System.Collections.Generic;
using Gate.TestHelpers;
using NUnit.Framework;

namespace Gate.Helpers.Tests
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
            var app = UrlMapper.New(new Dictionary<string,AppDelegate>());
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("404 NOTFOUND"));
            Assert.That(callResult.BodyText, Is.StringContaining("Not Found"));
        }
    }
}