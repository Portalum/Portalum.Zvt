using Portalum.Zvt.Models;
using Portalum.Zvt.Responses;
using System;

namespace Portalum.Zvt.Parsers
{
    public interface ITlvParser
    {
        bool Parse(byte[] data, IResponse response);
        TlvLengthInfo GetLength(Span<byte> data);

    }
}