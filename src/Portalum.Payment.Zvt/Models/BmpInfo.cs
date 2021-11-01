using Portalum.Payment.Zvt.Responses;
using System;

namespace Portalum.Payment.Zvt.Models
{
    public class BmpInfo
    {
        public byte Id { get; set; }
        public int DataLength { get; set; }

        public Func<byte[], int> CalculateDataLength { get; set; }

        public string Description { get; set; }

        public Func<byte[], IResponse, bool> TryParse;

        public override string ToString()
        {
            return $"{this.Description} - {this.Id:X2}";
        }
    }
}
