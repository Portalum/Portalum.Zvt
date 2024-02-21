using System;

namespace Portalum.Zvt.Parsers
{
    /// <summary>
    /// IntermediateStatusInformationParser Interface
    /// </summary>
    public interface IIntermediateStatusInformationParser
    {
        /// <summary>
        /// GetMessage
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        (byte StatusCode, string StatusInformation) GetMessage(Span<byte> data);
    }
}