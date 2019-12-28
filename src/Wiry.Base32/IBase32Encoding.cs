// Copyright (c) Dmitry Razumikhin, 2016-2019.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

namespace Wiry.Base32
{
    /// <summary>
    /// Base32 encoding interface.
    /// </summary>
    public interface IBase32Encoding
    {
        /// <summary>
        /// Encodes bytes to a string.
        /// </summary>
        string GetString(byte[] bytes, int index, int count);

        /// <summary>
        /// Decodes string data to bytes.
        /// </summary>
        byte[] ToBytes(string encoded, int index, int length);

        /// <summary>
        /// Validate input data.
        /// </summary>
        ValidationResult Validate(string encoded, int index, int length);
    }
}