// Copyright (c) Dmitry Razumikhin, 2016-2018.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

namespace Wiry.Base32
{
    /// <summary>
    /// Encoded data validation result.
    /// </summary>
    public enum ValidationResult
    {
        /// <summary>
        /// Data is correct.
        /// </summary>
        Ok = 1,

        /// <summary>
        /// Invalid arguments (null, out of range, etc).
        /// </summary>
        InvalidArguments = 2,

        /// <summary>
        /// Invalid data length.
        /// </summary>
        InvalidLength = 3,

        /// <summary>
        /// Invalid padding of the encoded data.
        /// </summary>
        InvalidPadding = 4,

        /// <summary>
        /// Character is not found in the encoding alphabet.
        /// </summary>
        InvalidCharacter = 5
    }
}