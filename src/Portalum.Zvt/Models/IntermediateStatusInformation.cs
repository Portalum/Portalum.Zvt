using Portalum.Zvt.Responses;

namespace Portalum.Zvt.Models
{
    public class IntermediateStatusInformation : IResponse, IResponseErrorMessage
    {
        public string ErrorMessage { get; set; }
    }
}
