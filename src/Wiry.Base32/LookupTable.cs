// Copyright (c) Dmitry Razumikhin, 2016-2017.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

namespace Wiry.Base32
{
    internal sealed class LookupTable
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