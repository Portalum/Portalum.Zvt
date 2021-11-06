using Portalum.Payment.Zvt.Models;
using System;

namespace Portalum.Payment.Zvt.Helpers
{
    /// <summary>
    /// BitHelper
    /// </summary>
    public static class BitHelper
    {
        /// <summary>
        /// Sets a specific bit in a byte
        /// </summary>
        /// <param name="b">Byte which is to be processed</param>
        /// <param name="bitIndex">Index</param>
        /// <returns></returns>
        public static byte SetBit(byte b, int bitIndex)
        {
            if (bitIndex < 8 && bitIndex > -1)
            {
                return (byte)(b | (byte)(0x01 << bitIndex));
            }

            throw new InvalidOperationException($"The value for BitNumber {bitIndex} was not in the permissible range! (BitNumber = (min)0 - (max)7)");
        }

        /// <summary>
        /// Get a specific bit in a byte
        /// </summary>
        /// <param name="b">Byte which is to be processed</param>
        /// <param name="bitIndex">Index</param>
        /// <returns></returns>
        public static bool GetBit(byte b, int bitIndex)
        {
            return (b & (1 << bitIndex)) != 0;
        }

        /// <summary>
        /// Get all bits in a byte
        /// </summary>
        /// <param name="b">Byte which is to be processed</param>
        /// <returns></returns>
        public static ByteBitInfo GetBits(byte b)
        {
            return new ByteBitInfo
            {
                Bit0 = GetBit(b, 0),
                Bit1 = GetBit(b, 1),
                Bit2 = GetBit(b, 2),
                Bit3 = GetBit(b, 3),
                Bit4 = GetBit(b, 4),
                Bit5 = GetBit(b, 5),
                Bit6 = GetBit(b, 6),
                Bit7 = GetBit(b, 7),
            };
        }
    }
}
