using System.Text;
using Wiry.Base32;
using Xunit;

namespace UnitTests
{
    public class Rfc4648Tests
    {
        // RFC 4648 Test Vectors
        // https://tools.ietf.org/html/rfc4648#section-10
        //
        // BASE32("") = ""
        // BASE32("f") = "MY======"
        // BASE32("fo") = "MZXQ===="
        // BASE32("foo") = "MZXW6==="
        // BASE32("foob") = "MZXW6YQ="
        // BASE32("fooba") = "MZXW6YTB"
        // BASE32("foobar") = "MZXW6YTBOI======"
        //
        //

        private static string Base32(string input)
        {
            return Base32Encoding.Standard.GetString(Encoding.ASCII.GetBytes(input));
        }

        [Fact]
        public void Base32_Test1()
        {
            Assert.Equal("", Base32(""));
        }

        [Fact]
        public void Base32_Test2()
        {
            Assert.Equal("MY======", Base32("f"));
        }

        [Fact]
        public void Base32_Test3()
        {
            Assert.Equal("MZXQ====", Base32("fo"));
        }

        [Fact]
        public void Base32_Test4()
        {
            Assert.Equal("MZXW6===", Base32("foo"));
        }

        [Fact]
        public void Base32_Test5()
        {
            Assert.Equal("MZXW6YQ=", Base32("foob"));
        }

        [Fact]
        public void Base32_Test6()
        {
            Assert.Equal("MZXW6YTB", Base32("fooba"));
        }

        [Fact]
        public void Base32_Test7()
        {
            Assert.Equal("MZXW6YTBOI======", Base32("foobar"));
        }
    }
}