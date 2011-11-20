using System;
using System.Collections.Generic;
using Gate.Middleware.Utils;
using NUnit.Framework;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class RangeHeaderTests
    {
        [Test]
        public void RangeHeader_Ignores_Syntactically_Invalid_Byte_Ranges()
        {
            Assert.IsNull(RangeHeader.Parse(new Dictionary<string, object> { {"owin.RequestHeaders", new Dictionary<string, string>()}}, 500));
            Assert.IsNull(CreateByteRanges("foobar", 500));
            Assert.IsNull(CreateByteRanges("furlongs=123-456", 500));
            Assert.IsNull(CreateByteRanges("bytes=", 500));
            Assert.IsNull(CreateByteRanges("bytes=-", 500));
            Assert.IsNull(CreateByteRanges("bytes=123,456", 500));

            // A range of non-positive length is syntactically invalid and ignored:
            Assert.IsNull(CreateByteRanges("bytes=456-123", 500));
            Assert.IsNull(CreateByteRanges("bytes=456-455", 500));
        }

        [Test]
        public void RangeHeader_Parses_Simple_Byte_Ranges()
        {
            CollectionAssert.AreEqual(new[] { new Tuple<long, long>(123, 456) }, CreateByteRanges("bytes=123-456", 500));
            CollectionAssert.AreEqual(new[] { new Tuple<long, long>(123, 499) }, CreateByteRanges("bytes=123-", 500));
            CollectionAssert.AreEqual(new[] { new Tuple<long, long>(400, 499) }, CreateByteRanges("bytes=-100", 500));
            CollectionAssert.AreEqual(new[] { new Tuple<long, long>(0, 0) }, CreateByteRanges("bytes=0-0", 500));
            CollectionAssert.AreEqual(new[] { new Tuple<long, long>(499, 499) }, CreateByteRanges("bytes=499-499", 500));
        }

        [Test]
        public void RangeHeader_Truncates_Byte_Ranges()
        {
            CollectionAssert.AreEqual(new[] { new Tuple<long, long>(123, 499) }, CreateByteRanges("bytes=123-999", 500));
            CollectionAssert.AreEqual(new long[] { }, CreateByteRanges("bytes=600-999", 500));
            CollectionAssert.AreEqual(new[] { new Tuple<long, long>(0, 499) }, CreateByteRanges("bytes=-999", 500));
        }

        [Test]
        public void RangeHeader_Ignores_Unsatisfiable_Byte_Ranges()
        {
            CollectionAssert.AreEqual(new long[] { }, CreateByteRanges("bytes=500-", 0));
            CollectionAssert.AreEqual(new long[] { }, CreateByteRanges("bytes=999-", 0));
            CollectionAssert.AreEqual(new long[] { }, CreateByteRanges("bytes=500-501", 0));
            CollectionAssert.AreEqual(new long[] { }, CreateByteRanges("bytes=-0", 0));
        }

        [Test]
        public void RangeHeader_Handles_Byte_Ranges_Of_Empty_Files()
        {
            CollectionAssert.AreEqual(new long[] { }, CreateByteRanges("bytes=123-456", 0));
            CollectionAssert.AreEqual(new long[] { }, CreateByteRanges("bytes=0-", 0));
            CollectionAssert.AreEqual(new long[] { }, CreateByteRanges("bytes=-100", 0));
            CollectionAssert.AreEqual(new long[] { }, CreateByteRanges("bytes=0-0", 0));
            CollectionAssert.AreEqual(new long[] { }, CreateByteRanges("bytes=-0", 0));
        }

        private static IEnumerable<Tuple<long, long>> CreateByteRanges(string rangeString, int size)
        {
            return RangeHeader.Parse(new Dictionary<string, object>
            {
                {"owin.RequestHeaders", new Dictionary<string, string>
                {
                    {"Range", rangeString}
                }}
            }, size);
        }
    }
}