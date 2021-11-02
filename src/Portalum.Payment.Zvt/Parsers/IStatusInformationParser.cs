using Portalum.Payment.Zvt.Models;
using System;

namespace Portalum.Payment.Zvt.Parsers
{
    /// <summary>
    /// StatusInformationParser Interface
    /// </summary>
    public interface IStatusInformationParser
    {
        /// <summary>
        /// Parse
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        StatusInformation Parse(Span<byte> data);
    }
}