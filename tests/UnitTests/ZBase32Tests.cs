// Copyright (c) Dmitry Razumikhin, 2016.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

using System;
using System.Text;
using Wiry.Base32;
using Xunit;

namespace UnitTests
{
    public class ZBase32Tests
    {
        #region z-base-32 Test Vectors (https://philzimmermann.com/docs/human-oriented-base-32-encoding.txt)

        // base32     z-base-32
        // 6C74O===   6n9hq
        // 2R5AI===   4t7ye

        [Fact]
        public void ZBase32_Test1()
        {
            byte[] bytes = Base32Encoding.Standard.ToBytes("6C74O===");
            string zb32 = Base32Encoding.ZBase32.GetString(bytes);
            Assert.Equal("6n9hq", zb32);
        }

        [Fact]
        public void ZBase32_Test2()
        {
            byte[] bytes = Base32Encoding.Standard.ToBytes("2R5AI===");
            string zb32 = Base32Encoding.ZBase32.GetString(bytes);
            Assert.Equal("4t7ye", zb32);
        }

        #endregion

        #region Empty tests

        [Fact]
        public void ZBase32_Test3()
        {
            Assert.Equal(string.Empty, Base32Encoding.ZBase32.GetString(new byte[0]));
        }

        [Fact]
        public void ZBase32_Test4()
        {
            Assert.Equal(0, Base32Encoding.ZBase32.ToBytes(string.Empty).Length);
        }

        #endregion

        #region Bad input tests

        [Fact]
        public void ZBase32_Test_BadChar()
        {
            Assert.Throws<FormatException>(() => Base32Encoding.ZBase32.ToBytes("4!7ye"));
        }

        [Fact]
        public void ZBase32_Test_1char()
        {
            Base32Encoding.ZBase32.ToBytes("m");
        }

        [Fact]
        public void ZBase32_Test_9chars()
        {
            Base32Encoding.ZBase32.ToBytes("gaaaaaaaa");
        }

        #endregion

        #region Complex tests

        private const string Complex1Text = "Lower case is easier to read. That's why people have " +
                                            "been using it preferentially since around the 9th century CE.";

        private const string Complex1ZBase32 = "jtzzq3m1rbtsnh5frbwzgedfcf3s13m1rb4g6ed1ciosemtyktwgn7b8qco8q4d3rbag" +
                                               "k55opt11y4dbq311yaufcizny7mupfzgqedjqoo8yhufc31zr3mqqtwsn5dcxro8g4mq" +
                                               "cp11yam1p74sh3byqtwgkeb3qtwnya5fp348khu3rbbwkmo";

        [Fact]
        public void ZBase32_Test_Complex1_Encode()
        {
            string zb = Base32Encoding.ZBase32.GetString(Encoding.ASCII.GetBytes(Complex1Text));
            Assert.Equal(Complex1ZBase32, zb);
        }

        [Fact]
        public void ZBase32_Test_Complex1_Decode()
        {
            string text = Encoding.ASCII.GetString(Base32Encoding.ZBase32.ToBytes(Complex1ZBase32));
            Assert.Equal(Complex1Text, text);
        }

        [Fact]
        public void ZBase32_Test_Complex2_Encode()
        {
            string zb = Base32Encoding.ZBase32.GetString(Encoding.ASCII.GetBytes("xyHelloz"), 2, 5);
            Assert.Equal("jb1sa5dx", zb);
        }

        [Fact]
        public void ZBase32_Test_Complex2_Decode()
        {
            string text = Encoding.ASCII.GetString(Base32Encoding.ZBase32.ToBytes("+++jb1sa5dx@@@@@@", 3, 8));
            Assert.Equal("Hello", text);
        }

        #endregion

        #region Fuzzy test

        [Fact]
        public void ZBase32_Test_Fuzzy1()
        {
            int repeats = 1000000;
            int minInputSize = 0;
            int maxInputSize = 100;
            const string alphabet = "ybndrfg8ejkmcpqxot1uwisza345h769";

            var rnd = new Random(20170125);
            for (int i = 0; i < repeats; i++)
            {
                int size = rnd.Next(minInputSize, maxInputSize + 1);
                var sb = new StringBuilder(size);
                for (int j = 0; j < size; j++)
                {
                    sb.Append(alphabet[rnd.Next(alphabet.Length)]);
                }
                string zbase32 = sb.ToString();
                Base32Encoding.ZBase32.ToBytes(zbase32); // crash or not to crash
            }
        }

        #endregion
    }
}