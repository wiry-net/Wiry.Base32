// Copyright (c) Dmitry Razumikhin, 2016-2019.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

using System;
using System.Text;
using Wiry.Base32;
using Xunit;

namespace UnitTests
{
    public class CustomBase32EncodingTests
    {
        private const string Rfc4648Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        private const string RussianAlphabet = "АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";

        [Fact]
        public void CustomEncodingTest()
        {
            const string text = "The Base 32 encoding is designed to represent arbitrary " +
                                "sequences of octets in a form that needs to be case insensitive " +
                                "but that need not be human readable.";

            var bytes = Encoding.ASCII.GetBytes(text);

            var standardEncoded = Base32Encoding.Standard.GetString(bytes);

            var russianBase32Encoding = new RussianBase32Encoding();
            var russianEncoded = russianBase32Encoding.GetString(bytes);
            Assert.Equal(standardEncoded, ReplaceWithRfc4648(russianEncoded));

            var roundTripText = Encoding.ASCII.GetString(russianBase32Encoding.ToBytes(russianEncoded));
            Assert.Equal(text, roundTripText);
        }

        [Fact]
        public void RepeatedAlphabetSymbolIsRejected()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => new CustomBase32Encoding("ABCDEFGHIJKLMNOPQRSTUVWXYZ23456A", '='));
            Assert.Equal("alphabet", ex.ParamName);
        }

        [Fact]
        public void PadSymbolInsideAlphabetIsRejected()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => new CustomBase32Encoding("ABCDEFGHIJKLMNOPQRSTUVWXYZ23456=", '='));
            Assert.Equal("padSymbol", ex.ParamName);
        }

        [Fact]
        public void AlphabetMayContainPadCharacterWhenUnpadded()
        {
            var encoding = new CustomBase32Encoding("ABCDEFGHIJKLMNOPQRSTUVWXYZ23456=", null);
            var bytes = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff };
            Assert.Equal("========", encoding.GetString(bytes));
            Assert.Equal(bytes, encoding.ToBytes("========"));
        }

        private static string ReplaceWithRfc4648(string russian)
        {
            var sb = new StringBuilder(russian);
            for (var i = 0; i < 32; i++)
            {
                sb.Replace(RussianAlphabet[i], Rfc4648Alphabet[i]);
            }

            return sb.ToString();
        }

        private class RussianBase32Encoding : CustomBase32Encoding
        {
            public RussianBase32Encoding() : base(RussianAlphabet, '=')
            {
            }
        }
    }
}