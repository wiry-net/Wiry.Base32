namespace Wiry.Base32
{
    internal sealed class ZBase32Encoding : Base32Encoding
    {
        protected override string Alphabet => "ybndrfg8ejkmcpqxot1uwisza345h769";

        public override string GetString(byte[] bytes, int index, int count)
        {
            return ToBase32(bytes, index, count, AlphabetCharArray, null);
        }
    }
}