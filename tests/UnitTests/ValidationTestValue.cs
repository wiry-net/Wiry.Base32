// Copyright (c) Dmitry Razumikhin, 2016-2018.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

using Wiry.Base32;

namespace UnitTests;

public class ValidationTestValue(ValidationResult result, Exception toBytesError)
{
    public ValidationResult Result { get; } = result;
    public Exception ToBytesError { get; } = toBytesError;
}
