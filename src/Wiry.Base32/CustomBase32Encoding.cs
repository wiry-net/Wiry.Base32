// Copyright (c) Dmitry Razumikhin, 2016-2019.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

using System;

namespace Wiry.Base32
{
    /// <summary>
    /// Base32 implementation based on a specified alphabet and padding symbol.
    /// </summary>
    public class CustomBase32Encoding : Base32Encoding
    {
        /// <summary>
        /// Initializes a new instance of the CustomBase32Encoding class with specified alphabet and padding symbol.
        /// </summary>
        /// <exception cref="ArgumentException">The alphabet is not 32 distinct symbols, or contains the padding symbol.</exception>
        public CustomBase32Encoding(string alphabet, char? padSymbol)
            : base(alphabet, padSymbol)
        {
        }
    }
}