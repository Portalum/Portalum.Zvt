using System.Linq;

namespace Portalum.Payment.Zvt.Models
{
    /// <summary>
    /// Zvt - Application Protocol Data Unit
    /// </summary>
    public class ApduResponseInfo
    {
        public byte[] ControlField { get; set; }
        public int DataLength { get; set; }
        public int DataStartIndex { get; set; }

        public bool CanHandle(params byte[] data)
        {
            return this.ControlField.SequenceEqual(data);
        }
    }
}
