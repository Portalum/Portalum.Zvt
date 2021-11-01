namespace Portalum.Payment.Zvt.Models
{
    public class TlvTagFieldInfo
    {
        public TlvTagFieldClassType ClassType { get; set; }
        public TlvTagFieldDataObjectType DataObjectType { get; set; }
        public int TagNumber { get; set; }
        public string Tag { get; set; }
        public int NumberOfBytesThatCanBeSkipped { get; set; }

        public override string ToString()
        {
            return $"ClassType:{this.ClassType} DataObjectType:{this.DataObjectType} TagNumber:{this.TagNumber} Tag:{this.Tag} NumberOfBytesThatCanBeSkipped:{this.NumberOfBytesThatCanBeSkipped}";
        }
    }
}
