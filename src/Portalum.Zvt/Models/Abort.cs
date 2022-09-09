using Portalum.Zvt.Responses;

namespace Portalum.Zvt.Models
{
    public class Abort : IResponse,
        IResponseErrorCode,
        IResponseErrorMessage
    {
        public byte ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}
