namespace Portalum.Payment.Zvt.Models
{
    public class PrintLineInfo
    {
        public bool IsTextCentred { get; set; }
        public bool IsDoubleWidth { get; set; }
        public bool IsDoubleHeight { get; set; }
        public bool IsLastLine { get; set; }
        public string Text { get; set; }
    }
}
