using System.Collections.Generic;

namespace Gate.TestHelpers
{
    public class FakeHostRequest : Environment
    {
        public FakeHostRequest(IDictionary<string, object> env) : base(env)
        {
        }
    }
}