namespace Portalum.Payment.Zvt.Models
{
    public class TlvLengthInfo
    {
        public bool Successful { get; set; }
        public int Length { get; set; }
        public int NumberOfBytesThatCanBeSkipped { get; set; }
    }
}
