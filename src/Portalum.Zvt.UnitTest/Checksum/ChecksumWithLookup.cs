using System;

namespace Portalum.Zvt.UnitTest
{
    public static class ChecksumWithLookup
    {
        private static ushort[] LookupTable;

        /// <summary>
        /// Create LookupTable
        /// </summary>
        public static void CreateLookupTable()
        {
            LookupTable = new ushort[256];
            var truncatedPolynomial = (ushort)0x8408;

            for (ushort i = 0; i <= 255; i++)
            {
                ushort value = i;
                for (var bit = 1; bit <= 8; bit++)
                {
                    if ((value & 1) != 1)
                    {
                        value /= 2;
                    }
                    else
                    {
                        value = (ushort)(value / 2 ^ truncatedPolynomial);
                    }
                }
                LookupTable[i] = value;
            }
        }

        /// <summary>
        /// Calculate Checksum
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] ComputeHash(Span<byte> data)
        {
            var max = 256;
            var temp = 0;

            int lowByte;
            int highByte;

            for (var i = 0; i < data.Length; i++)
            {
                highByte = temp / max;
                lowByte = temp - max * highByte;
                temp = LookupTable[lowByte ^ data[i]] ^ highByte;
            }

            highByte = temp / max;
            lowByte = temp - max * highByte;

            return new byte[] { (byte)lowByte, (byte)highByte };
        }
    }
}
