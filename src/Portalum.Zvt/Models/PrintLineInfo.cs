using Portalum.Zvt.Responses;

namespace Portalum.Zvt.Models
{
    /// <summary>
    /// Print LineInfo
    /// </summary>
    public class PrintLineInfo : IResponse
    {
        /// <summary>
        /// Is text centred
        /// </summary>
        public bool IsTextCentred { get; set; }

        /// <summary>
        /// Is double width
        /// </summary>
        public bool IsDoubleWidth { get; set; }

        /// <summary>
        /// Is double height
        /// </summary>
        public bool IsDoubleHeight { get; set; }

        /// <summary>
        /// Is last line
        /// </summary>
        public bool IsLastLine { get; set; }

        /// <summary>
        /// Text
        /// </summary>
        public string Text { get; set; }
    }
}
