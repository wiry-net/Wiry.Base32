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