// Copyright (c) Dmitry Razumikhin, 2016-2026.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Wiry.Base32;
using Xunit;

namespace UnitTests
{
    public class EncodedLengthTests
    {
        // A whole number of bytes encodes to 0, 2, 4, 5 or 7 symbols past the last full group.
        private static bool Expected(int length) => new[] { 0, 2, 4, 5, 7 }.Contains(length % 8);

        private static IEnumerable<int> Range(long from, long to)
        {
            for (long length = from; length <= to; length++)
                yield return (int)length;
        }

        [Fact]
        public void IsEncodedLength_AgreesWithTailRule_AcrossTheIntRange()
        {
            // 429496730 is the first length where length * 5 no longer fits in an int.
            var lengths = Range(0, 4096)
                .Concat(Range(429496729L - 64, 429496729L + 64))
                .Concat(Range(int.MaxValue - 64L, int.MaxValue));

            var wrong = lengths.Where(length => Base32Encoding.IsEncodedLength(length) != Expected(length)).ToList();

            Assert.Empty(wrong);
        }
    }
}
