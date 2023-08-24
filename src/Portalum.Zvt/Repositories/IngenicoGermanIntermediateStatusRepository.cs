using System.Collections.Generic;

namespace Portalum.Zvt.Repositories
{
    /// <summary>
    /// Ingenico German IntermediateStatusRepository
    /// </summary>
    public class IngenicoGermanIntermediateStatusRepository : EnglishIntermediateStatusRepository
    {
        /// <inheritdoc/>
        public IngenicoGermanIntermediateStatusRepository() : base(new Dictionary<byte, string>
        {
            { 0xA0, "Zahlung erfolgt" },
            { 0xA1, "Zahlung erfolgt\nBitte Karte entnehmen!" },
            { 0xA2, "Storno erfolgt" },
            { 0xA3, "Storno erfolgt\nBitte Karte entnehmen!" },
            { 0xA4, "Storno nicht möglich" },
            { 0xA5, "Storno nicht möglich\nBitte Karte entnehmen!" }
        })
        { }
    }
}
