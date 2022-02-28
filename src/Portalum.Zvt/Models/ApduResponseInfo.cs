using System.Linq;

namespace Portalum.Zvt.Models
{
    /// <summary>
    /// Zvt - Application Protocol Data Unit
    /// </summary>
    public class ApduResponseInfo
    {
        /// <summary>
        /// ControlField
        /// </summary>
        public byte[] ControlField { get; set; }

        /// <summary>
        /// DataLength
        /// </summary>
        /// <remarks>
        /// MaxLenght (default 255, with ExtendedLengthFieldIndicator 65535)
        /// </remarks>
        public int DataLength { get; set; }

        /// <summary>
        /// Start position of data part
        /// </summary>
        public int DataStartIndex { get; set; }

        /// <summary>
        /// Full package size
        /// </summary>
        public int PackageSize
        {
            get
            {
                return this.DataStartIndex + this.DataLength;
            }
        }

        /// <summary>
        /// Check data package can handle
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool CanHandle(params byte[] data)
        {
            return this.ControlField.SequenceEqual(data);
        }
    }
}
