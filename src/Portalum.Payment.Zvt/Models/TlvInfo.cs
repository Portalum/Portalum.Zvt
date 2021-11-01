using Portalum.Payment.Zvt.Responses;
using System;

namespace Portalum.Payment.Zvt.Models
{
    public class TlvInfo
    {
        public string Tag { get; set; }

        public string Description { get; set; }

        public Func<byte[], IResponse, bool> TryProcess;

        public override string ToString()
        {
            return $"{this.Description} - {this.Tag}";
        }
    }
}
