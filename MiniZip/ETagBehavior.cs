namespace Knapcode.MiniZip
{
    /// <summary>
    /// How to use ETag headers when reading ZIP metadata over HTTP.
    /// </summary>
    public enum ETagBehavior
    {
        /// <summary>
        /// If there is a non-weak ETag header, use it.
        /// </summary>
        UseIfPresent = 0,

        /// <summary>
        /// Don't use the ETag header at all.
        /// </summary>
        Ignore = 1,

        /// <summary>
        /// Require a non-weak ETag header.
        /// </summary>
        Required = 2,
    }
}
