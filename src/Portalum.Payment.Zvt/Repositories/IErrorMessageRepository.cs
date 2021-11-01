namespace Portalum.Payment.Zvt.Repositories
{
    public interface IErrorMessageRepository
    {
        string GetMessage(byte key);
    }
}
