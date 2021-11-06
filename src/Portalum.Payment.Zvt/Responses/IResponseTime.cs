using System;

namespace Portalum.Payment.Zvt.Responses
{
    public interface IResponseTime
    {
        TimeSpan Time { get; set; }
    }
}
