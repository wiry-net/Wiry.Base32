// Copyright (c) Dmitry Razumikhin, 2016-2018.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

namespace Wiry.Base32;

internal sealed class LookupTable(int lowCode, int[] values)
{
    public int LowCode { get; } = lowCode;
    public int[] Values { get; } = values;
}
