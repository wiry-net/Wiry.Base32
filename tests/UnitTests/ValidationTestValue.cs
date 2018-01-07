// Copyright (c) Dmitry Razumikhin, 2016-2018.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

using System;
using Wiry.Base32;

namespace UnitTests
{
    public class ValidationTestValue
    {
        public ValidationResult Result { get; }
        public Exception ToBytesError { get; }

        public ValidationTestValue(ValidationResult result, Exception toBytesError)
        {
            Result = result;
            ToBytesError = toBytesError;
        }
    }
}