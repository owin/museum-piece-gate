﻿using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Nancy.Hosting.Owin.Tests.Fakes;

namespace Gate.TestHelpers
{
    public class FakeHostResponse
    {
        public string Status { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public BodyDelegate Body { get; set; }
        public string BodyText { get; set; }
        public XElement BodyXml { get; set; }

        public FakeConsumer Consumer { get; set; }
        public Exception Exception { get; set; }
    }
}