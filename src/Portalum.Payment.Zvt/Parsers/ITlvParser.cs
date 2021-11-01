using Portalum.Payment.Zvt.Models;
using Portalum.Payment.Zvt.Responses;
using System;

namespace Portalum.Payment.Zvt.Parsers
{
    public interface ITlvParser
    {
        bool Parse(byte[] data, IResponse response);
        TlvLengthInfo GetLength(Span<byte> data);

    }
}