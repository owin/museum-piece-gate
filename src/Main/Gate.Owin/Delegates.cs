using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Owin
{
    public delegate void AppDelegate(
        IDictionary<string, object> env,
        ResultDelegate result,
        Action<Exception> fault);

    public delegate void ResultDelegate(
        string status,
        IDictionary<string, IEnumerable<string>> headers,
        BodyDelegate body);

    public delegate void BodyDelegate(
        Func<ArraySegment<byte>, bool> write,
        Func<Action, bool> flush,
        Action<Exception> end,
        CancellationToken cancellationToken);

    public delegate Task<Tuple<string /* status */, IDictionary<String, IEnumerable<string>> /* headers */, BodyDelegate /* body */>>
        AppTaskDelegate(IDictionary<string, object> env);
}
