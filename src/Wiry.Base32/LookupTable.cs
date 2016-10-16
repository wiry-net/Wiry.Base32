namespace Wiry.Base32
{
    public sealed class LookupTable
    {
        public int LowCode { get; }
        public int[] Values { get; }

        public LookupTable(int lowCode, int[] values)
        {
            LowCode = lowCode;
            Values = values;
        }
    }
}