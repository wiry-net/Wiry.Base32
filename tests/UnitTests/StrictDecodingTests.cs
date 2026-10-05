// Copyright (c) Dmitry Razumikhin, 2016-2026.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Wiry.Base32;
using Xunit;

namespace UnitTests
{
    public class StrictDecodingTests
    {
        private const string Rfc = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        private static readonly Dictionary<string, Base32Encoding> All = new Dictionary<string, Base32Encoding>
        {
            ["standard"] = Base32Encoding.Standard,
            ["z-base-32"] = Base32Encoding.ZBase32,
            ["custom hex, pad #"] = new CustomBase32Encoding("0123456789ABCDEFGHIJKLMNOPQRSTUV", '#'),
            ["custom nano, no pad"] = new CustomBase32Encoding("13456789abcdefghijkmnopqrstuwxyz", null),
            ["lenient rfc"] = new CustomBase32Encoding(Rfc, '=', true),
            ["lenient z-base-32"] = new CustomBase32Encoding("ybndrfg8ejkmcpqxot1uwisza345h769", null, true),
        };

        public static IEnumerable<object[]> Encodings() => All.Keys.Select(name => new object[] { name });

        [Theory]
        [InlineData("standard", "MZXQ====", ValidationResult.Ok, "666f")]
        [InlineData("standard", "MZXW6===", ValidationResult.Ok, "666f6f")]
        [InlineData("standard", "M=======", ValidationResult.InvalidPadding, "padding")]
        [InlineData("standard", "!=======", ValidationResult.InvalidPadding, "padding")]
        [InlineData("standard", "MZXW6Y==", ValidationResult.InvalidPadding, "padding")]
        [InlineData("standard", "MZX=====", ValidationResult.InvalidPadding, "padding")]
        [InlineData("standard", "MZXR====", ValidationResult.InvalidCharacter, "trailing")]
        [InlineData("standard", "MZXW7===", ValidationResult.InvalidCharacter, "trailing")]
        [InlineData("standard", "MZXW6YQ=", ValidationResult.Ok, "666f6f62")]
        [InlineData("standard", "MZXW6YR=", ValidationResult.InvalidCharacter, "trailing")]
        [InlineData("lenient rfc", "MZXR====", ValidationResult.Ok, "666f")]
        [InlineData("lenient rfc", "MZX=====", ValidationResult.InvalidPadding, "padding")]
        [InlineData("z-base-32", "yr", ValidationResult.Ok, "01")]
        [InlineData("z-base-32", "yn", ValidationResult.InvalidCharacter, "trailing")]
        [InlineData("z-base-32", "y", ValidationResult.InvalidLength, "length")]
        [InlineData("z-base-32", "yyy", ValidationResult.InvalidLength, "length")]
        [InlineData("z-base-32", "yyyyyy", ValidationResult.InvalidLength, "length")]
        [InlineData("z-base-32", "=", ValidationResult.InvalidLength, "length")]
        [InlineData("lenient z-base-32", "yn", ValidationResult.Ok, "00")]
        [InlineData("lenient z-base-32", "yyy", ValidationResult.InvalidLength, "length")]
        public void Decode_EachClass(string name, string input, ValidationResult validation, string bytesOrError)
        {
            Base32Encoding encoding = All[name];

            Assert.Equal(validation, encoding.Validate(input));
            if (validation == ValidationResult.Ok)
            {
                Assert.Equal(bytesOrError, string.Concat(encoding.ToBytes(input).Select(b => b.ToString("x2"))));
            }
            else
            {
                var error = Assert.Throws<FormatException>(() => encoding.ToBytes(input));
                Assert.Contains(bytesOrError, error.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static IEnumerable<string> Inputs(Base32Encoding encoding, Random random)
        {
            string symbols = encoding.GetString(Enumerable.Range(0, 255).Select(i => (byte)i).ToArray());
            symbols = new string(symbols.Distinct().ToArray()) + "==##!";
            for (int length = 0; length <= 40; length++)
            {
                byte[] data = new byte[length];
                random.NextBytes(data);
                string valid = encoding.GetString(data);
                yield return valid;
                for (int cut = 0; cut < valid.Length; cut++)
                    yield return valid.Substring(0, cut);

                for (int i = 0; i < 30; i++)
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
        public void Validate_IsOkExactlyWhenToBytesSucceeds(string name)
        {
            Base32Encoding encoding = All[name];
            var disagreements = new List<string>();
            foreach (string input in Inputs(encoding, new Random(name.Length)))
            {
                byte[] bytes = null;
                try
                {
                    bytes = encoding.ToBytes(input);
                }
                catch (FormatException)
                {
                }

                if ((encoding.Validate(input) == ValidationResult.Ok) != (bytes != null))
                    disagreements.Add(input);
            }

            Assert.Empty(disagreements);
        }

        [Theory]
        [InlineData("standard")]
        [InlineData("z-base-32")]
        [InlineData("custom hex, pad #")]
        [InlineData("custom nano, no pad")]
        public void StrictDecoding_AcceptsOnlyCanonicalStrings(string name)
        {
            Base32Encoding encoding = All[name];
            var aliases = new List<string>();
            int accepted = 0;
            foreach (string input in Inputs(encoding, new Random(name.Length)))
            {
                byte[] bytes;
                try
                {
                    bytes = encoding.ToBytes(input);
                }
                catch (FormatException)
                {
                    continue;
                }

                accepted++;
                if (encoding.GetString(bytes) != input)
                    aliases.Add(input);
            }

            Assert.Empty(aliases);
            Assert.True(accepted > 1000, $"only {accepted} inputs accepted");
        }
    }
}
