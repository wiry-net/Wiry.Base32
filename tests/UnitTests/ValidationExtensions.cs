// Copyright (c) Dmitry Razumikhin, 2016-2018.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

using System;
using Wiry.Base32;

namespace UnitTests
{
    public static class ValidationExtensions
    {
        public static ValidationTestValue CheckValidation(this Base32Encoding encoder, string encoded)
        {
            Exception toBytesError = null;
            try
            {
                encoder.ToBytes(encoded);
            }
            catch (Exception ex)
            {
                toBytesError = ex;
            }

            var validationResult = encoder.Validate(encoded);
            var vr2 = encoder.Validate(encoded, 0, encoded?.Length ?? 0);
            var vr3 = encoder.Validate(encoded != null ? "test" + encoded : null, 4, encoded?.Length ?? 0);
            if (validationResult != vr2 || validationResult != vr3)
                throw new Exception("CheckValidation error");

            return new ValidationTestValue(validationResult, toBytesError);
        }
    }
}