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
            return new ValidationTestValue(validationResult, toBytesError);
        }
    }
}