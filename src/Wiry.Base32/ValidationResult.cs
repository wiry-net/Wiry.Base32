namespace Wiry.Base32
{
    /// <summary>
    /// 
    /// </summary>
    public enum ValidationResult
    {
        /// <summary>
        /// 
        /// </summary>
        Ok = 1,

        /// <summary>
        /// 
        /// </summary>
        InvalidArguments = 2,

        /// <summary>
        /// 
        /// </summary>
        InvalidLength = 3,

        /// <summary>
        /// 
        /// </summary>
        InvalidPadding = 4,

        /// <summary>
        /// 
        /// </summary>
        InvalidCharacter = 5
    }
}