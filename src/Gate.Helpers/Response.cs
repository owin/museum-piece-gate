using System;
using System.Collections.Generic;

namespace Gate.Helpers
{
    public class Response
    {
        public Response(Action<string, IDictionary<string, string>, Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action>> result)
        {
            throw new NotImplementedException();
        }

        public Response Write(string text)
        {
            throw new NotImplementedException();
        }

        public void Finish()
        {
            throw new NotImplementedException();
        }

        public void Finish(Action<Action<Exception>, Action> body)
        {
            throw new NotImplementedException();
        }
    }
}