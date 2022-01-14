namespace Portalum.Zvt.Responses
{
    public interface IResponseCardholderAuthentication
    {
        string CardholderAuthentication { get; set; }
        bool PrintoutNeeded { get; set; }
    }
}
