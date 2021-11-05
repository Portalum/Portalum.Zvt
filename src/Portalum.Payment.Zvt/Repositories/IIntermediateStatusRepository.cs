namespace Portalum.Payment.Zvt.Repositories
{
    public interface IIntermediateStatusRepository
    {
        string GetMessage(byte key);
    }
}
