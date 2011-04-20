using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gate.Utils
{
    using BodyDelegate = Func< // body
        Func< // next
            ArraySegment<byte>, // data
            Action, // continuation
            bool>, // async                    
        Action<Exception>, // error
        Action, // complete
        Action>; //cancel

    public static class Body
    {
        public static BodyDelegate FromText(string text)
        {
            return FromText(text, Encoding.UTF8);
        }

        public static BodyDelegate FromText(string text, Encoding encoding)
        {
            return (data, error, complete) =>
            {
                if (!data(new ArraySegment<byte>(encoding.GetBytes(text)), complete))
                    complete();

                return () => { };
            };
        }
    }
}