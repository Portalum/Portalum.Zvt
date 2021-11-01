namespace Portalum.Payment.Zvt.UnitTest
{
    public static class ChecksumWithLookup
    {
        private static ushort[] LookupTable;

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

        public static byte[] ComputeHash(byte[] data)
        {
            var max = 256;
            var temp = 0;
            var lowByte = 0;
            var highByte = 0;

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
