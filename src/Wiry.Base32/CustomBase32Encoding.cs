// Copyright (c) Dmitry Razumikhin, 2016-2019.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

using System;

namespace Wiry.Base32
{
    /// <summary>
    /// Base32 implementation based on specified <see cref="Alphabet"/> and <see cref="PadSymbol"/>.
    /// </summary>
    public class CustomBase32Encoding : Base32Encoding
    {
        /// <summary>
        /// Alphabet of a concrete Base32 encoding.
        /// </summary>
        protected override string Alphabet { get; }

        /// <summary>
        /// Padding symbol of a concrete Base32 encoding.
        /// </summary>
        protected override char? PadSymbol { get; }

        /// <summary>
        /// Initializes a new instance of the CustomBase32Encoding class with specified alphabet and padding symbol.
        /// </summary>
        public CustomBase32Encoding(string alphabet, char? padSymbol)
        {
            if (alphabet == null)
                throw new ArgumentNullException(nameof(alphabet));

            if (alphabet.Length != AlphabetLength)
            {
                throw new ArgumentOutOfRangeException
                (
                    nameof(alphabet),
                    alphabet.Length,
                    $"The length of the alphabet must be {AlphabetLength}, but {alphabet.Length} is specified."
                );
            }

            Alphabet = alphabet;
            PadSymbol = padSymbol;
        }
    }
}