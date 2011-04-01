using System;
using System.Collections.Generic;
using Nancy;

namespace Sample.AspNet {
        
    public class MainModule : NancyModule {
        public MainModule() {
            Get["/"] = parameters => { return View["staticview"]; };
            Post["/"] = parameters => { return Response.AsXml(new Foo { Hello = (string)parameters.Something }); };
        }

        public class Foo {
            public string Hello { get; set; }
        }
    }
}