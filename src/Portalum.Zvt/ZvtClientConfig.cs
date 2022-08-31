using System;

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

        /// <summary>
        /// Timeout after Command Acknowledge and Completion
        /// </summary>
        public TimeSpan CommandCompletionTimeout = TimeSpan.FromSeconds(180);

        
        /// <summary>
        /// Maximum number of times the AskForCompletionInfo callback is called. After this number of
        /// times a auto reversal will be triggered and the payment will fail. A PT usually queries
        /// every 2 (ZVT standard) to 4 (observed in practice) seconds for the completion status.
        /// </summary>
        public byte GetAsyncCompletionInfoLimit = 10;
    }
}
