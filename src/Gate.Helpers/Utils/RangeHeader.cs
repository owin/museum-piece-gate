using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Gate.Helpers.Utils
{
    public class RangeHeader
    {
        public static IEnumerable<Tuple<long, long>> Parse(IDictionary<string, object> env, long size)
        {
            var headers = new Environment(env).Headers;
            var httpRange = headers.ContainsKey("Range") ? headers["Range"] : null;

            if (httpRange == null)
            {
                return null;
            }

            var ranges = Enumerable.Empty<Tuple<long, long>>();

            var rangeSpecs = Regex.Split(httpRange, @",\s*");
            foreach (var rangeSpec in rangeSpecs)
            {
                var regex = new Regex(@"bytes=(\d*)-(\d*)");
                var matches = regex.Matches(rangeSpec);

                if (matches.Count == 0 || matches[0].Groups.Count == 0)
                {
                    return null;
                }

                var groups = matches[0].Groups;

                if (groups.Count <= 1)
                {
                    return null;
                }

                var r0 = groups[1].Value;
                var r1 = groups[2].Value;
                long r0Value;
                long r1Value;

                if (r0 == string.Empty)
                {
                    if (r1 == string.Empty)
                    {
                        return null;
                    }

                    // suffix-byte-range-spec, represents trailing suffix of file
                    r0Value = new[] { size - long.Parse(r1), 0 }.Max<long>();
                    r1Value = size - 1;
                }
                else
                {
                    r0Value = long.Parse(r0);

                    if (r1 == string.Empty)
                    {
                        r1Value = size - 1;
                    }
                    else
                    {
                        r1Value = long.Parse(r1);

                        if (r1Value < r0Value)
                        {
                            // backwards range is syntactically invalid
                            return null;
                        }

                        if (r1Value >= size)
                        {
                            r1Value = size - 1;
                        }
                    }
                }

                if (r0Value <= r1Value)
                {
                    ranges.Concat(new[] { new Tuple<long, long>(r0Value, r1Value) });
                }
            }

            return ranges;
        }
    }
}