using Portalum.Payment.Zvt.Models;
using System;

namespace Portalum.Payment.Zvt.Parsers
{
    /// <summary>
    /// PrintTextBlockParser Interface
    /// </summary>
    public interface IPrintTextBlockParser
    {
        /// <summary>
        /// Parse
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        ReceiptInfo Parse(Span<byte> data);
    }
}