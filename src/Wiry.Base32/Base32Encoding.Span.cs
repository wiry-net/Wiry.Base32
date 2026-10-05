// Copyright (c) Dmitry Razumikhin, 2016-2026.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

#if NETSTANDARD2_1
using System;

namespace Wiry.Base32
{
    public abstract partial class Base32Encoding
    {
        /// <summary>
        /// Gets the exact number of symbols that encoding <paramref name="bytesCount"/> bytes produces.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="bytesCount"/> is negative, or the result does not fit in an <see cref="int"/>.
        /// </exception>
        public int GetEncodedLength(int bytesCount)
        {
            if (bytesCount < 0)
                throw new ArgumentOutOfRangeException(nameof(bytesCount));

            long length = ComputeEncodedLength(bytesCount, PadSymbol);
            if (length > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(bytesCount));

            return (int)length;
        }

        /// <summary>
        /// Gets the largest number of bytes that decoding <paramref name="encodedLength"/> symbols can produce.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="encodedLength"/> is negative.</exception>
        public int GetMaxDecodedLength(int encodedLength)
        {
            if (encodedLength < 0)
                throw new ArgumentOutOfRangeException(nameof(encodedLength));

            return (int)((long)encodedLength * 5 / 8);
        }

        /// <summary>
        /// Encodes bytes to a string.
        /// </summary>
        public unsafe string GetString(ReadOnlySpan<byte> bytes)
        {
            if (bytes.IsEmpty)
                return string.Empty;

            int length = GetEncodedLength(bytes.Length);
            fixed (byte* pBytes = bytes)
            {
                // The span is pinned for the whole call, so the callback may read it through the pointer.
                var state = (Encoding: this, Bytes: (IntPtr)pBytes, Count: bytes.Length);
                return string.Create(length, state, (chars, s) =>
                    s.Encoding.Encode(new ReadOnlySpan<byte>((void*)s.Bytes, s.Count), chars));
            }
        }

        /// <summary>
        /// Encodes bytes into <paramref name="destination"/>.
        /// </summary>
        /// <returns>
        /// false, with <paramref name="charsWritten"/> set to 0, when <paramref name="destination"/> is shorter than
        /// <see cref="GetEncodedLength"/> of the input.
        /// </returns>
        public bool TryGetChars(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten)
        {
            long length = ComputeEncodedLength(bytes.Length, PadSymbol);
            if (length > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            Encode(bytes, destination);
            charsWritten = (int)length;
            return true;
        }

        /// <summary>
        /// Decodes symbols to bytes.
        /// </summary>
        /// <exception cref="FormatException">The input is not valid for this encoding.</exception>
        public unsafe byte[] ToBytes(ReadOnlySpan<char> encoded)
        {
            if (encoded.IsEmpty)
                return Array.Empty<byte>();

            LookupTable lookupTable = GetOrCreateLookupTable(Alphabet);
            fixed (char* pEncoded = encoded)
            {
                int bytesCount = GetBytesCountWithChecks(pEncoded, encoded.Length, PadSymbol, out int groupsCount,
                    out int remainder);

                var bytes = new byte[bytesCount];
                if (bytesCount > 0)
                {
                    fixed (byte* pOutput = bytes)
                    {
                        ToBytesUnsafe(pEncoded, pOutput, groupsCount, remainder, lookupTable);
                    }
                }

                return bytes;
            }
        }

        /// <summary>
        /// Decodes symbols into <paramref name="destination"/>.
        /// </summary>
        /// <returns>
        /// false, with <paramref name="bytesWritten"/> set to 0, when <paramref name="destination"/> is shorter than
        /// the decoded data. <see cref="GetMaxDecodedLength"/> gives a length that is always enough.
        /// </returns>
        /// <exception cref="FormatException">The input is not valid for this encoding.</exception>
        public unsafe bool TryGetBytes(ReadOnlySpan<char> encoded, Span<byte> destination, out int bytesWritten)
        {
            bytesWritten = 0;
            if (encoded.IsEmpty)
                return true;

            LookupTable lookupTable = GetOrCreateLookupTable(Alphabet);
            fixed (char* pEncoded = encoded)
            {
                int bytesCount = GetBytesCountWithChecks(pEncoded, encoded.Length, PadSymbol, out int groupsCount,
                    out int remainder);

                if (bytesCount > destination.Length)
                    return false;

                if (bytesCount > 0)
                {
                    fixed (byte* pOutput = destination)
                    {
                        ToBytesUnsafe(pEncoded, pOutput, groupsCount, remainder, lookupTable);
                    }
                }

                bytesWritten = bytesCount;
                return true;
            }
        }

        /// <summary>
        /// Validate input data.
        /// </summary>
        public unsafe ValidationResult Validate(ReadOnlySpan<char> encoded)
        {
            LookupTable lookupTable = GetOrCreateLookupTable(Alphabet);
            fixed (char* pEncoded = encoded)
            {
                return ValidateUnsafe(pEncoded, encoded.Length, PadSymbol, lookupTable);
            }
        }

        private static long ComputeEncodedLength(long bytesCount, char? padSymbol)
        {
            long remainder = bytesCount % 5;
            long length = bytesCount / 5 * 8;
            if (padSymbol == null)
                return length + GetSymbolsCount((int)remainder);

            return remainder == 0 ? length : length + 8;
        }

        private unsafe void Encode(ReadOnlySpan<byte> bytes, Span<char> destination)
        {
            if (bytes.IsEmpty)
                return;

            string alphabet = Alphabet;
            if (alphabet == null)
                throw new ArgumentNullException(nameof(alphabet));

            if (alphabet.Length < AlphabetLength)
                throw new ArgumentException("Alphabet length must be greater or equal than 32");

            fixed (byte* pInput = bytes)
            fixed (char* pOutput = destination)
            {
                ToBase32Unsafe(pInput, pOutput, bytes.Length / 5, bytes.Length % 5, alphabet, PadSymbol);
            }
        }
    }
}
#endif
