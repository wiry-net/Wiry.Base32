using System;
using System.Text;
using Wiry.Base32;
using Xunit;

namespace UnitTests
{
    public class Rfc4648Tests
    {
        #region RFC 4648 Test Vectors (https://tools.ietf.org/html/rfc4648#section-10)

        [Fact]
        public void Base32_Test1_Encode()
        {
            Assert.Equal("", EncodeBase32(""));
        }

        [Fact]
        public void Base32_Test1_Decode()
        {
            Assert.Equal("", DecodeBase32(""));
        }

        [Fact]
        public void Base32_Test2_Encode()
        {
            Assert.Equal("MY======", EncodeBase32("f"));
        }

        [Fact]
        public void Base32_Test2_Decode()
        {
            Assert.Equal("f", DecodeBase32("MY======"));
        }

        [Fact]
        public void Base32_Test3_Encode()
        {
            Assert.Equal("MZXQ====", EncodeBase32("fo"));
        }

        [Fact]
        public void Base32_Test3_Decode()
        {
            Assert.Equal("fo", DecodeBase32("MZXQ===="));
        }

        [Fact]
        public void Base32_Test4_Encode()
        {
            Assert.Equal("MZXW6===", EncodeBase32("foo"));
        }

        [Fact]
        public void Base32_Test4_Decode()
        {
            Assert.Equal("foo", DecodeBase32("MZXW6==="));
        }

        [Fact]
        public void Base32_Test5_Encode()
        {
            Assert.Equal("MZXW6YQ=", EncodeBase32("foob"));
        }

        [Fact]
        public void Base32_Test5_Decode()
        {
            Assert.Equal("foob", DecodeBase32("MZXW6YQ="));
        }

        [Fact]
        public void Base32_Test6_Encode()
        {
            Assert.Equal("MZXW6YTB", EncodeBase32("fooba"));
        }

        [Fact]
        public void Base32_Test6_Decode()
        {
            Assert.Equal("fooba", DecodeBase32("MZXW6YTB"));
        }

        [Fact]
        public void Base32_Test7_Encode()
        {
            Assert.Equal("MZXW6YTBOI======", EncodeBase32("foobar"));
        }

        [Fact]
        public void Base32_Test7_Decode()
        {
            Assert.Equal("foobar", DecodeBase32("MZXW6YTBOI======"));
        }

        #endregion

        #region Bad input tests

        [Fact]
        public void Base32_Test_SingleChar()
        {
            Assert.Throws<FormatException>(() => DecodeBase32("M"));
        }

        [Fact]
        public void Base32_Test_IncompletePadding()
        {
            Assert.Throws<FormatException>(() => DecodeBase32("MZXW6YTBOI====="));
        }

        [Fact]
        public void Base32_Test_PlainPadding()
        {
            Assert.Throws<FormatException>(() => DecodeBase32("========"));
        }

        [Fact]
        public void Base32_Test_PaddingInsideText()
        {
            Assert.Throws<FormatException>(() => DecodeBase32("MZX=6YQ="));
        }

        [Fact]
        public void Base32_Test_BadCharInsideAlphabetRange()
        {
            Assert.Throws<FormatException>(() => DecodeBase32("MZX96YQ="));
        }

        [Fact]
        public void Base32_Test_BadCharOutsideAlphabetRangeLeft()
        {
            Assert.Throws<FormatException>(() => DecodeBase32("MZX$6YQ="));
        }

        [Fact]
        public void Base32_Test_BadCharOutsideAlphabetRangeRight()
        {
            Assert.Throws<FormatException>(() => DecodeBase32("MZX~6YQ="));
        }

        #endregion

        #region Complex tests

        private const string Complex1Text = "The Base 32 encoding is designed to represent arbitrary " +
                                            "sequences of octets in a form that needs to be case insensitive " +
                                            "but that need not be human readable.";

        private const string Complex1Base32 = "KRUGKICCMFZWKIBTGIQGK3TDN5SGS3THEBUXGIDEMVZWSZ3OMVSCA5DP" +
                                              "EBZGK4DSMVZWK3TUEBQXEYTJORZGC4TZEBZWK4LVMVXGGZLTEBXWMIDP" +
                                              "MN2GK5DTEBUW4IDBEBTG64TNEB2GQYLUEBXGKZLEOMQHI3ZAMJSSAY3B" +
                                              "ONSSA2LOONSW443JORUXMZJAMJ2XIIDUNBQXIIDOMVSWIIDON52CAYTF" +
                                              "EBUHK3LBNYQHEZLBMRQWE3DFFY======";

        [Fact]
        public void Base32_Test_Complex1_Encode()
        {
            Assert.Equal(Complex1Base32, EncodeBase32(Complex1Text));
        }

        [Fact]
        public void Base32_Test_Complex1_Decode()
        {
            Assert.Equal(Complex1Text, DecodeBase32(Complex1Base32));
        }

        [Fact]
        public void Base32_Test_Complex2_Encode()
        {
            string b1 = EncodeBase32("bar");
            string b2 = EncodeBase32("foobar", 3, 3);
            Assert.Equal(b1, b2);
        }

        public void Base32_Test_Complex2_Decode()
        {
            Assert.Equal("foobar", DecodeBase32("introMZXW6YTBOI======SomeData", 5, 16));
        }

        #endregion

        private static string EncodeBase32(string input)
        {
            return Base32Encoding.Standard.GetString(Encoding.ASCII.GetBytes(input));
        }

        private static string EncodeBase32(string input, int index, int count)
        {
            return Base32Encoding.Standard.GetString(Encoding.ASCII.GetBytes(input), index, count);
        }

        private static string DecodeBase32(string encoded)
        {
            return Encoding.ASCII.GetString(Base32Encoding.Standard.ToBytes(encoded));
        }

        private static string DecodeBase32(string encoded, int index, int length)
        {
            return Encoding.ASCII.GetString(Base32Encoding.Standard.ToBytes(encoded, index, length));
        }
    }
}