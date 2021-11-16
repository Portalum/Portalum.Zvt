using Portalum.Zvt.Models;
using System;

namespace Portalum.Zvt.Parsers
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