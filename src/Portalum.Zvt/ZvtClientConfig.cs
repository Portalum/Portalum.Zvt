namespace Portalum.Zvt
{
    /// <summary>
    /// ZVT Client Config
    /// </summary>
    public class ZvtClientConfig
    {
        /// <summary>
        /// Terminal Password
        /// </summary>
        public int Password { get; set; } = 000000;
        /// <summary>
        ///  Lanugage for Translation Repositories
        /// </summary>
        public Language Language { get; set; } = Language.English;
        /// <summary>
        /// ZVT Encoding
        /// </summary>
        public ZvtEncoding Encoding { get; set; } = ZvtEncoding.CodePage437;
    }
}
