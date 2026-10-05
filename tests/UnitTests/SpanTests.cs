// Copyright (c) Dmitry Razumikhin, 2016-2026.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

#if NETCOREAPP3_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Wiry.Base32;
using Xunit;

namespace UnitTests
{
    public class SpanTests
    {
        private static readonly Dictionary<string, Base32Encoding> All = new Dictionary<string, Base32Encoding>
        {
            ["standard"] = Base32Encoding.Standard,
            ["z-base-32"] = Base32Encoding.ZBase32,
            ["custom hex, pad #"] = new CustomBase32Encoding("0123456789ABCDEFGHIJKLMNOPQRSTUV", '#'),
            ["custom nano, no pad"] = new CustomBase32Encoding("13456789abcdefghijkmnopqrstuwxyz", null),
        };

        public static IEnumerable<object[]> Encodings() => All.Keys.Select(name => new object[] { name });

        private static string Outcome(Func<byte[]> decode)
        {
            try
            {
                return "ok " + Convert.ToHexString(decode());
            }
            catch (Exception ex)
            {
                return ex.GetType().Name;
            }
        }

        private static IEnumerable<string> Inputs(Base32Encoding encoding, Random random)
        {
            string symbols = encoding.GetString(Enumerable.Range(0, 255).Select(i => (byte)i).ToArray()) + "=#!a\u00e9";
            for (int length = 0; length <= 40; length++)
            {
                byte[] data = new byte[length];
                random.NextBytes(data);
                string valid = encoding.GetString(data);
                yield return valid;
                for (int cut = 0; cut < valid.Length; cut++)
                    yield return valid.Substring(0, cut);

                for (int i = 0; i < 20; i++)
                {
                    char[] mutated = valid.ToCharArray();
                    if (mutated.Length > 0)
                        mutated[random.Next(mutated.Length)] = symbols[random.Next(symbols.Length)];
                    yield return new string(mutated);
                    yield return new string(Enumerable.Range(0, length).Select(_ => symbols[random.Next(symbols.Length)]).ToArray());
                }
            }
        }

        [Theory]
        [MemberData(nameof(Encodings))]
        public void Encode_SpanOverloadsMatchArrayApi(string name)
        {
            Base32Encoding encoding = All[name];
            var random = new Random(name.Length);
            for (int length = 0; length <= 80; length++)
            {
                for (int round = 0; round < 10; round++)
                {
                    byte[] buffer = new byte[length + 6];
                    random.NextBytes(buffer);
                    int index = random.Next(7);
                    string expected = encoding.GetString(buffer, index, length);
                    ReadOnlySpan<byte> window = buffer.AsSpan(index, length);

                    Assert.Equal(expected, encoding.GetString(window));
                    Assert.Equal(expected.Length, encoding.GetEncodedLength(length));

                    char[] destination = new char[expected.Length + 3];
                    Assert.True(encoding.TryGetChars(window, destination, out int written));
                    Assert.Equal(expected, new string(destination, 0, written));
                }
            }
        }

        [Theory]
        [MemberData(nameof(Encodings))]
        public void Decode_SpanOverloadsMatchArrayApi(string name)
        {
            Base32Encoding encoding = All[name];
            var random = new Random(name.Length);
            foreach (string input in Inputs(encoding, random))
            {
                string padded = "#=" + input + "a=";
                string expected = Outcome(() => encoding.ToBytes(input));

                Assert.Equal(expected, Outcome(() => encoding.ToBytes(padded.AsSpan(2, input.Length))));
                Assert.Equal(expected, Outcome(() =>
                {
                    byte[] destination = new byte[encoding.GetMaxDecodedLength(input.Length)];
                    Assert.True(encoding.TryGetBytes(padded.AsSpan(2, input.Length), destination, out int written));
                    return destination.AsSpan(0, written).ToArray();
                }));
                Assert.Equal(encoding.Validate(input), encoding.Validate(padded.AsSpan(2, input.Length)));
            }
        }

        [Theory]
        [MemberData(nameof(Encodings))]
        public void ShortDestination_ReturnsFalseAndWritesNothing(string name)
        {
            Base32Encoding encoding = All[name];
            byte[] data = { 1, 2, 3, 4, 5, 6 };
            string encoded = encoding.GetString(data);

            char[] chars = Enumerable.Repeat('*', encoded.Length - 1).ToArray();
            Assert.False(encoding.TryGetChars(data, chars, out int charsWritten));
            Assert.Equal(0, charsWritten);
            Assert.All(chars, ch => Assert.Equal('*', ch));

            byte[] bytes = Enumerable.Repeat((byte)0xAA, data.Length - 1).ToArray();
            Assert.False(encoding.TryGetBytes(encoded, bytes, out int bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.All(bytes, b => Assert.Equal(0xAA, b));
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Header
        {
            public int Id;
            public long Stamp;
            public byte Flags;
        }

        [Fact]
        public void Struct_RoundTripsThroughMemoryMarshal()
        {
            var header = new Header { Id = 42, Stamp = -7, Flags = 0x81 };

            string encoded = Base32Encoding.Standard.GetString(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)));
            var decoded = default(Header);
            Assert.True(Base32Encoding.Standard.TryGetBytes(encoded, MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref decoded, 1)), out int written));

            Assert.Equal(13, written);
            Assert.Equal(header, decoded);
        }
    }
}
#endif
