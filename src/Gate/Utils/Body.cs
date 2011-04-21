using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gate.Utils
{
    public static class Body
    {
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