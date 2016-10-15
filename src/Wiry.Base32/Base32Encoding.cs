using System;
using System.Runtime.CompilerServices;

namespace Wiry.Base32
{
    public abstract class Base32Encoding
    {
        private static volatile Base32Encoding _standard;
        private static volatile Base32Encoding _zBase32;

        public static Base32Encoding Standard => _standard ?? (_standard = new StandardBase32Encoding());
        public static Base32Encoding ZBase32 => _zBase32 ?? (_zBase32 = new ZBase32Encoding());

        public virtual string GetString(byte[] bytes)
        {
            return GetString(bytes, 0, bytes.Length);
        }

        public abstract string GetString(byte[] bytes, int index, int count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetSymbolsCount(int bytesCount)
        {
            return (bytesCount * 8 + 4) / 5;
        }

        internal static unsafe void ToBase32GroupsUnsafe(byte* pInput, char* pOutput, char* pAlphabet,
            int inputGroupsCount)
        {
            for (int i = 0; i < inputGroupsCount; i++)
            {
                ulong value = *pInput++;
                for (int j = 0; j < 4; j++)
                {
                    value <<= 8;
                    value |= *pInput++;
                }
                pOutput += 7;
                char* pNextPos = pOutput + 1;
                for (int j = 0; j < 7; j++)
                {
                    *pOutput-- = pAlphabet[value & 0x1F];
                    value >>= 5;
                }
                *pOutput = pAlphabet[value];
                pOutput = pNextPos;
            }
        }

        internal static unsafe int ToBase32RemainderUnsafe(byte* pInput, char* pOutput, char* pAlphabet, int remainder)
        {
            ulong value = *pInput++;
            for (int j = 1; j < remainder; j++)
            {
                value <<= 8;
                value |= *pInput++;
            }

            int symbols = GetSymbolsCount(remainder);
            value <<= (5 - remainder) * 8 - (8 - symbols) * 5; // RemainderShiftTable = { 0, 2, 4, 1, 3 };

            pOutput += symbols - 1;
            for (int j = 1; j < symbols; j++)
            {
                *pOutput-- = pAlphabet[value & 0x1F];
                value >>= 5;
            }
            *pOutput = pAlphabet[value];

            return symbols;
        }

        internal static unsafe void ToBase32Unsafe(byte[] input, int inputOffset, char[] output, int outputOffset,
            int inputGroupsCount, int remainder, string alphabet, char? padSymbol)
        {
            fixed (byte* pInput = &input[inputOffset])
            fixed (char* pOutput = &output[outputOffset])
            fixed (char* pAlphabet = alphabet)
            {
                ToBase32GroupsUnsafe(pInput, pOutput, pAlphabet, inputGroupsCount);
                if (remainder <= 0)
                    return;

                byte* pInputRemainder = pInput + inputGroupsCount * 5;
                char* pOutputRemainder = pOutput + inputGroupsCount * 8;
                int symbols = ToBase32RemainderUnsafe(pInputRemainder, pOutputRemainder, pAlphabet, remainder);
                if (padSymbol == null)
                    return;

                char padChar = padSymbol.Value;
                pOutputRemainder += symbols;
                for (int i = symbols; i < 8; i++)
                {
                    *pOutputRemainder++ = padChar;
                }
            }
        }

        public static string ToBase32(byte[] buffer, int offset, int count, string alphabet, char? padSymbol)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentException(nameof(offset));

            if (count < 0 || count > buffer.Length - offset)
                throw new ArgumentException(nameof(count));

            if (alphabet == null)
                throw new ArgumentNullException(nameof(alphabet));

            if (alphabet.Length < 32)
                throw new ArgumentException("Alphabet length must be greater or equal than 32");

            if (count == 0)
                return string.Empty;

            int groupsCount = count / 5;
            int remainder = count % 5;

            int symbolsCount = groupsCount * 8;
            if (padSymbol == null)
            {
                symbolsCount += GetSymbolsCount(remainder);
            }
            else if (remainder != 0)
            {
                symbolsCount += 8;
            }

            var symbols = new char[symbolsCount];
            ToBase32Unsafe(buffer, offset, symbols, 0, groupsCount, remainder, alphabet, padSymbol);
            return new string(symbols);
        }
    }
}