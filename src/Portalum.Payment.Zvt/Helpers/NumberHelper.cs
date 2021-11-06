using System;
using System.Linq;

namespace Portalum.Payment.Zvt.Helpers
{
    /// <summary>
    /// NumberHelper
    /// </summary>
    public static class NumberHelper
    {
        /// <summary>
        /// Get number of digits
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static int GetNumberOfDigits(decimal value)
        {
            var abs = Math.Abs(value);

            return abs < 1 ? 0 : (int)(Math.Log10(decimal.ToDouble(abs)) + 1);
        }

        /// <summary>
        /// Convert decimal to Binary Coded Decimal
        /// </summary>
        /// <param name="value"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] DecimalToBcd(decimal value, int length = 6)
        {
            var x = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
            var x1 = (long)(x * 100);

            if (GetNumberOfDigits(value) > 10)
            {
                return new byte[0];
            }

            var data = new byte[length];

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(x1 % 10);
                x1 /= 10;

                data[i] |= (byte)((x1 % 10) << 4);
                x1 /= 10;
            }

            return data.Reverse().ToArray();
        }

        /// <summary>
        /// Convert int to Binary Coded Decimal
        /// </summary>
        /// <param name="value"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] IntToBcd(int value, int length = 3)
        {
            var data = new byte[length];

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(value % 10);
                value /= 10;

                data[i] |= (byte)((value % 10) << 4);
                value /= 10;
            }

            return data.Reverse().ToArray();
        }

        /// <summary>
        /// Convert Binary Coded Decimal to int
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int BcdToInt(Span<byte> data)
        {
            var tempNumber = ByteHelper.ByteArrayToHex(data);
            if (int.TryParse(tempNumber, out var number))
            {
                return number;
            }

            return 0;
        }

        public static short ToInt16LittleEndian(Span<byte> data)
        {
            var tempData = data.ToArray();
            Array.Reverse(tempData);
            return BitConverter.ToInt16(tempData);
        }

        public static int BoolArrayToInt(params bool[] boolArray)
        {
            if (boolArray.Length > 31)
            {
                throw new ApplicationException("Too many elements to be converted to a single int");
            }

            var val = 0;
            for (var i = 0; i < boolArray.Length; ++i)
            {
                if (boolArray[i]) val |= 1 << i;
            }

            return val;
        }
    }
}
