using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gate.Owin
{
    public delegate void AppDelegate(
        IDictionary<string, object> env,
        ResultDelegate result,
        Action<Exception> fault);

    public delegate void ResultDelegate(
        string status,
        IDictionary<string, IEnumerable<string>> headers,
        BodyDelegate body);

    public delegate Action /* cancel */ BodyDelegate(
        Func<
            ArraySegment<byte>, // data
            Action, // continuation
            bool> // continuation was or will be invoked
            next,
        Action<Exception> error,
        Action complete);

    public delegate Task<Tuple<string /* status */, IDictionary<String, IEnumerable<string>> /* headers */, BodyDelegate /* body */>> 
        AppTaskDelegate(IDictionary<string, object> env);
}
