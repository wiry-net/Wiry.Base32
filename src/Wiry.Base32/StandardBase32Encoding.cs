// Copyright (c) Dmitry Razumikhin, 2016-2019.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

namespace Wiry.Base32
{
    internal sealed class StandardBase32Encoding : Base32Encoding
    {
        protected override string Alphabet => "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        protected override char? PadSymbol => '=';

        public override ValidationResult Validate(string encoded, int index, int length)
        {
            return Validate(encoded, index, length, PadSymbol, GetOrCreateLookupTable(Alphabet));
        }
    }
}