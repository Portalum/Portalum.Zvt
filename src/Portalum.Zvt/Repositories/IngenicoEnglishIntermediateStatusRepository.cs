using System.Collections.Generic;

namespace Portalum.Zvt.Repositories
{
    /// <summary>
    /// Ingenico English IntermediateStatusRepository
    /// </summary>
    public class IngenicoEnglishIntermediateStatusRepository : EnglishIntermediateStatusRepository
    {
        /// <inheritdoc/>
        public IngenicoEnglishIntermediateStatusRepository() : base(new Dictionary<byte, string>
        {
            { 0xA0, "Payment processed" },
            { 0xA1, "Payment processed\nPlease remove card!" },
            { 0xA2, "Cancellation successful" },
            { 0xA3, "Cancellation successful\nPlease remove card!" },
            { 0xA4, "Cancellation not possible" },
            { 0xA5, "Cancellation not possible\nPlease remove card!" }
        })
        { }
    }
}
