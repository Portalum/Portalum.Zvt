using Portalum.Zvt.Models;

namespace Portalum.Zvt.Repositories
{
    public interface IErrorMessageRepository
    {
        (byte StatusCode, string StatusInformation) GetMessage(byte key);
    }
}
