// Copyright (c) Dmitry Razumikhin, 2016-2019.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

namespace Wiry.Base32
{
    internal sealed class StandardBase32Encoding : Base32Encoding
    {
        public StandardBase32Encoding()
            : base("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567", '=')
        {
        }
    }
}