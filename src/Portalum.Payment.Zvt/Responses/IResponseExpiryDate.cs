namespace Portalum.Payment.Zvt.Responses
{
    public interface IResponseExpiryDate
    {
        public int ExpiryDateYear { get; set; }
        public int ExpiryDateMonth { get; set; }
    }
}
