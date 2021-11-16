using Microsoft.Extensions.Logging;
using Portalum.Zvt.Helpers;
using Portalum.Zvt.Models;
using System;
using System.Text;

namespace Portalum.Zvt.Parsers
{
    /// <summary>
    /// PrintLineParser
    /// </summary>
    public class PrintLineParser : IPrintLineParser
    {
        private readonly ILogger _logger;

        /// <summary>
        /// PrintLineParser
        /// </summary>
        /// <param name="logger"></param>
        public PrintLineParser(
            ILogger logger)
        {
            this._logger = logger;
        }

        /// <inheritdoc />
        public PrintLineInfo Parse(Span<byte> data)
        {
            /*
            Attribute Definition
            1000 0000 RFU
            1xxx xxxx (not equal to 80h) this is the last line
            1111 1111 Linefeed, count of feeds follows
            01xx nnnn centred
            0x1x nnnn double width
            0xx1 nnnn double height
            0000 nnnn normal text
            */
            var attribute = data.Slice(0, 1);
            var bits = BitHelper.GetBits(attribute[0]);

            var text = Encoding.UTF7.GetString(data.Slice(1));

            return new PrintLineInfo
            {
                IsLastLine = bits.Bit7,
                IsTextCentred = bits.Bit6,
                IsDoubleWidth = bits.Bit5,
                IsDoubleHeight = bits.Bit4,
                Text = text
            };
        }
    }
}
