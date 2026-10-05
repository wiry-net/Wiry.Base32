// Copyright (c) Dmitry Razumikhin, 2016-2026.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

using System.Text;
using Wiry.Base32;
using Xunit;

namespace UnitTests
{
    public class CompatibilityTests
    {
        private static readonly byte[] Foobar = Encoding.ASCII.GetBytes("foobar");

        public static TheoryData<IBase32Encoding, string> Encodings => new TheoryData<IBase32Encoding, string>
        {
            { Base32Encoding.Standard, "MZXW6YTBOI======" },
            { Base32Encoding.ZBase32, "c3zs6aubqe" },
            { new CustomBase32Encoding("0123456789ABCDEFGHIJKLMNOPQRSTUV", '='), "CPNMUOJ1E8======" },
            { new LowerCaseStandard(), "mzxw6ytboi======" },
        };

        [Fact]
        public void SubclassWrittenFor111KeepsWorking()
        {
            Base32Encoding encoding = new LowerCaseStandard();

            Assert.Equal("mzxw6ytboi======", encoding.GetString(Foobar));
            Assert.Equal(Foobar, encoding.ToBytes("mzxw6ytboi======"));
            Assert.Equal(ValidationResult.Ok, encoding.Validate("mzxw6ytboi======"));
            Assert.Equal(ValidationResult.InvalidCharacter, encoding.Validate("mzxw6ytbo1======"));
        }

        [Theory]
        [MemberData(nameof(Encodings))]
        public void WindowedCallsThroughInterface(IBase32Encoding encoding, string encoded)
        {
            var bytes = new byte[Foobar.Length + 2];
            Foobar.CopyTo(bytes, 1);
            var text = "!" + encoded + "!";

            Assert.Equal(encoded, encoding.GetString(bytes, 1, Foobar.Length));
            Assert.Equal(Foobar, encoding.ToBytes(text, 1, encoded.Length));
            Assert.Equal(ValidationResult.Ok, encoding.Validate(text, 1, encoded.Length));
        }

        // Shaped the way 1.1.1 forced a subclass to be written: it overrides exactly the three
        // members that were abstract there. Any 1.x release must compile and run it unchanged.
        private sealed class LowerCaseStandard : Base32Encoding
        {
            public override string GetString(byte[] bytes, int index, int count) =>
                Standard.GetString(bytes, index, count).ToLowerInvariant();

            public override byte[] ToBytes(string encoded, int index, int length) =>
                Standard.ToBytes(encoded.ToUpperInvariant(), index, length);

            public override ValidationResult Validate(string encoded, int index, int length) =>
                Standard.Validate(encoded.ToUpperInvariant(), index, length);
        }
    }
}
