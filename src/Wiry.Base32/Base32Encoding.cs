// Copyright (c) Dmitry Razumikhin, 2016-2019.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Wiry.Base32
{
    /// <summary>
    /// Generic Base32 implementation
    /// </summary>
    public abstract class Base32Encoding : IBase32Encoding
    {
        private const string ErrorMessageInvalidLength = "Invalid length";
        private const string ErrorMessageInvalidPadding = "Invalid padding";
        private const string ErrorMessageInvalidCharacter = "Invalid character";

        private static volatile Base32Encoding _standard;
        private static volatile Base32Encoding _zBase32;

        /// <summary>
        /// Represent standard encoding defined in RFC 4648
        /// </summary>
        public static Base32Encoding Standard => _standard ?? (_standard = new StandardBase32Encoding());

        /// <summary>
        /// Represent z-base-32 encoding by Zooko O'Whielacronx
        /// </summary>
        public static Base32Encoding ZBase32 => _zBase32 ?? (_zBase32 = new ZBase32Encoding());

        private volatile LookupTable _lookupTable;

        /// <summary>
        /// Alphabet of a concrete Base32 encoding.
        /// </summary>
        protected abstract string Alphabet { get; }

        /// <summary>
        /// Padding symbol of a concrete Base32 encoding.
        /// </summary>
        protected abstract char? PadSymbol { get; }

        /// <summary>
        /// Get encoded string
        /// </summary>
        public virtual string GetString(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            return GetString(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// When overridden in a derived class, encodes bytes a string.
        /// </summary>
        public virtual string GetString(byte[] bytes, int index, int count)
        {
            return ToBase32(bytes, index, count, Alphabet, PadSymbol);
        }

        /// <summary>
        /// When overridden in a derived class, decodes string data to bytes.
        /// </summary>
        public virtual byte[] ToBytes(string encoded)
        {
            if (encoded == null)
                throw new ArgumentNullException(nameof(encoded));

            return ToBytes(encoded, 0, encoded.Length);
        }

        /// <summary>
        /// When overridden in a derived class, decodes string data to bytes.
        /// </summary>
        public abstract byte[] ToBytes(string encoded, int index, int length);

        /// <summary>
        /// Validate input data.
        /// </summary>
        public virtual ValidationResult Validate(string encoded)
        {
            if (encoded == null)
                return ValidationResult.InvalidArguments;

            return Validate(encoded, 0, encoded.Length);
        }

        /// <summary>
        /// Validate input data
        /// </summary>
        public abstract ValidationResult Validate(string encoded, int index, int length);

        internal LookupTable GetOrCreateLookupTable(string alphabet)
        {
            return _lookupTable ?? (_lookupTable = BuildLookupTable(alphabet));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetSymbolsCount(int bytesCount)
        {
            return (bytesCount * 8 + 4) / 5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetBytesCount(int symbolsCount)
        {
            return symbolsCount * 5 / 8;
        }

        private const int LookupTableNullItem = -1;

        private static LookupTable BuildLookupTable(string alphabet)
        {
            int[] codes = alphabet.Select(ch => (int)ch).ToArray();
            int min = codes.Min();
            int max = codes.Max();
            int size = max - min + 1;
            var table = new int[size];

            for (int i = 0; i < table.Length; i++)
                table[i] = LookupTableNullItem;

            foreach (int code in codes)
                table[code - min] = alphabet.IndexOf((char)code);

            return new LookupTable(min, table);
        }

        private static unsafe void ToBase32GroupsUnsafe(byte* pInput, char* pOutput, char* pAlphabet,
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

        private static unsafe int ToBase32RemainderUnsafe(byte* pInput, char* pOutput, char* pAlphabet, int remainder)
        {
            ulong value = *pInput++;
            for (int j = 1; j < remainder; j++)
            {
                value <<= 8;
                value |= *pInput++;
            }

            int symbols = GetSymbolsCount(remainder);
            value <<= (5 - remainder) * 8 - (8 - symbols) * 5;

            pOutput += symbols - 1;
            for (int j = 1; j < symbols; j++)
            {
                *pOutput-- = pAlphabet[value & 0x1F];
                value >>= 5;
            }

            *pOutput = pAlphabet[value];

            return symbols;
        }

        private static unsafe void ToBase32Unsafe(byte[] input, int inputOffset, char[] output, int outputOffset,
            int inputGroupsCount, int remainder, string alphabet, char? padSymbol)
        {
            fixed (byte* pInput = &input[inputOffset])
            fixed (char* pOutput = &output[outputOffset])
            fixed (char* pAlphabet = alphabet)
            {
                if (inputGroupsCount > 0)
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

        private static unsafe void ToBytesGroupsUnsafe(char* pEncoded, byte* pOutput, int encodedGroupsCount,
            int* pLookup, int lookupSize, int lowCode)
        {
            ulong value = 0;
            for (int i = encodedGroupsCount; i != 0; i--)
            {
                for (int j = 8; j != 0; j--)
                {
                    int lookupIndex = *pEncoded - lowCode;
                    if (lookupIndex < 0 || lookupIndex >= lookupSize)
                        throw new FormatException(ErrorMessageInvalidCharacter);

                    int item = *(pLookup + lookupIndex);
                    if (item == LookupTableNullItem)
                        throw new FormatException(ErrorMessageInvalidCharacter);

                    value <<= 5;
                    value |= (byte)item;
                    pEncoded++;
                }

                pOutput += 4;
                byte* pNextPos = pOutput + 1;
                for (int j = 4; j != 0; j--)
                {
                    *pOutput-- = (byte)value;
                    value >>= 8;
                }

                *pOutput = (byte)value;
                pOutput = pNextPos;
            }
        }

        private static unsafe void ToBytesRemainderUnsafe(char* pEncoded, byte* pOutput, int remainder,
            int* pLookup, int lookupSize, int lowCode)
        {
            ulong value = 0;
            for (int j = remainder; j != 0; j--)
            {
                int lookupIndex = *pEncoded - lowCode;
                if (lookupIndex < 0 || lookupIndex >= lookupSize)
                    throw new FormatException(ErrorMessageInvalidCharacter);

                int item = *(pLookup + lookupIndex);
                if (item == LookupTableNullItem)
                    throw new FormatException(ErrorMessageInvalidCharacter);

                value <<= 5;
                value |= (byte)item;
                pEncoded++;
            }

            int bytesCount = GetBytesCount(remainder);
            value >>= (5 - bytesCount) * 8 - (8 - remainder) * 5;

            bytesCount--;
            pOutput += bytesCount;
            for (int j = bytesCount; j != 0; j--)
            {
                *pOutput-- = (byte)value;
                value >>= 8;
            }

            *pOutput = (byte)value;
        }

        private static unsafe void ToBytesUnsafe(string encoded, int index, byte[] output, int outputOffset,
            int encodedGroupsCount, int remainder, LookupTable lookupTable)
        {
            int[] lookupValues = lookupTable.Values;
            int lowCode = lookupTable.LowCode;
            fixed (char* pEncodedBegin = encoded)
            fixed (byte* pOutput = &output[outputOffset])
            fixed (int* pLookup = lookupValues)
            {
                char* pEncoded = pEncodedBegin + index;
                if (encodedGroupsCount > 0)
                {
                    ToBytesGroupsUnsafe(pEncoded, pOutput, encodedGroupsCount, pLookup,
                        lookupValues.Length, lowCode);
                }

                if (remainder <= 1)
                    return;

                char* pEncodedRemainder = pEncoded + encodedGroupsCount * 8;
                byte* pOutputRemainder = pOutput + encodedGroupsCount * 5;
                ToBytesRemainderUnsafe(pEncodedRemainder, pOutputRemainder, remainder, pLookup,
                    lookupValues.Length, lowCode);
            }
        }

        internal static string ToBase32(byte[] bytes, int index, int count, string alphabet, char? padSymbol)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (count < 0 || count > bytes.Length - index)
                throw new ArgumentOutOfRangeException(nameof(count));

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
            ToBase32Unsafe(bytes, index, symbols, 0, groupsCount, remainder, alphabet, padSymbol);
            return new string(symbols);
        }

        private static void CheckToBytesArguments(string encoded, int index, int length, LookupTable lookupTable)
        {
            if (encoded == null)
                throw new ArgumentNullException(nameof(encoded));

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (length < 0 || length > encoded.Length - index)
                throw new ArgumentOutOfRangeException(nameof(length));

            if (lookupTable == null)
                throw new ArgumentNullException(nameof(lookupTable));
        }

        private static int GetRemainderWithChecks(string encoded, int index, int length, char? padSymbol)
        {
            if (padSymbol == null)
                return length % 8;

            if (length % 8 != 0)
                throw new FormatException(ErrorMessageInvalidLength);

            int remainder = 8;
            char padChar = padSymbol.Value;
            for (int i = index + length - 1; i >= index; i--)
            {
                if (encoded[i] != padChar)
                    break;

                if (--remainder <= 0)
                    throw new FormatException(ErrorMessageInvalidPadding);
            }

            return remainder;
        }

        internal static byte[] ToBytes(string encoded, int index, int length, char? padSymbol, LookupTable lookupTable)
        {
            CheckToBytesArguments(encoded, index, length, lookupTable);

            if (length == 0)
                return new byte[0];

            int remainder = GetRemainderWithChecks(encoded, index, length, padSymbol);

            int groupsCount = length / 8;

            int bytesCount = 0;
            if (remainder > 0)
            {
                if (padSymbol != null)
                {
                    // groupsCount always >= 1 here because of "length % 8 != 0" as checked before
                    groupsCount--;
                }

                bytesCount = GetBytesCount(remainder);
            }

            bytesCount += groupsCount * 5;

            var bytes = new byte[bytesCount];
            if (bytesCount > 0)
            {
                ToBytesUnsafe(encoded, index, bytes, 0, groupsCount, remainder, lookupTable);
            }

            return bytes;
        }

        internal static ValidationResult Validate(string encoded, int index, int length, char? padSymbol,
            LookupTable lookupTable)
        {
            try
            {
                CheckToBytesArguments(encoded, index, length, lookupTable);

                int bytesCount = GetBytesCount(length);
                int symbolsCount = GetSymbolsCount(bytesCount);
                if (symbolsCount != length)
                    return ValidationResult.InvalidLength;

                int remainder;
                try
                {
                    remainder = GetRemainderWithChecks(encoded, index, length, padSymbol);
                }
                catch (FormatException fex)
                {
                    switch (fex.Message)
                    {
                        case ErrorMessageInvalidLength:
                            return ValidationResult.InvalidLength;

                        case ErrorMessageInvalidPadding:
                            return ValidationResult.InvalidPadding;

                        default:
                            throw;
                    }
                }

                if (padSymbol != null)
                {
                    length -= 8 - remainder; // ignore padding
                }

                if (!CheckAlphabet(encoded, index, length, lookupTable))
                    return ValidationResult.InvalidCharacter;

                return ValidationResult.Ok;
            }
            catch
            {
                return ValidationResult.InvalidArguments;
            }
        }

        private static unsafe bool CheckAlphabet(string encoded, int index, int length, LookupTable lookupTable)
        {
            int[] lookupValues = lookupTable.Values;
            int lowCode = lookupTable.LowCode;
            int lookupSize = lookupValues.Length;
            fixed (char* pEncodedBegin = encoded)
            fixed (int* pLookup = lookupValues)
            {
                char* pEncoded = pEncodedBegin + index;
                char* pEnd = pEncoded + length;
                while (pEncoded < pEnd)
                {
                    int lookupIndex = *pEncoded - lowCode;
                    if (lookupIndex < 0 || lookupIndex >= lookupSize)
                        return false;

                    int item = *(pLookup + lookupIndex);
                    if (item == LookupTableNullItem)
                        return false;

                    pEncoded++;
                }
            }

            return true;
        }
    }
}