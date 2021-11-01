using System.Collections.Generic;

namespace Portalum.Payment.Zvt.Helpers
{
    public static class ChecksumHelper
    {
        public static ushort CalcCrc2(IEnumerable<byte> data)
        {
            int crc;
            var t = new int[256];

            for (var i = 0; i < 256; i++)
            {
                crc = i;

                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) > 0) crc = (crc >> 1) ^ 0x8408;
                    else crc = crc >> 1;
                }

                t[i] = crc;
            }

            crc = 0;

            var l = new List<byte>(data);
            l.Add(0x03);

            for (int i = 0; i < l.Count; i++)
            {
                var hb = crc >> 8;
                var lb = crc & 0xFF;

                crc = t[lb ^ l[i]] ^ hb;
            }

            return (ushort)((crc >> 8) | ((crc & 0xFF) << 8));
        }
    }
}
