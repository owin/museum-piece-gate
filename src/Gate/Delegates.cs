using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gate
{ 
    public delegate Action /* cancel */ BodyDelegate(
        Func<
            ArraySegment<byte>, // data
            Action, // continuation
            bool> // continuation was or will be invoked
            onNext,
        Action<Exception> onError,
        Action onComplete);
    
    public delegate void AppDelegate(
        IDictionary<string, object> env,
        ResultDelegate result,
        Action<Exception> fault);

    public delegate void ResultDelegate(
        string status,
        IDictionary<string, string> headers,
        Func<
            Func< // next
                ArraySegment<byte>, // data
                Action, // continuation
                bool>, // async                    
            Action<Exception>, // error
            Action, // complete
            Action> // cancel
        body); 
}
