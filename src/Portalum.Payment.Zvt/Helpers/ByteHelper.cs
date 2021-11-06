using System;
using System.Linq;
using System.Text;

namespace Portalum.Payment.Zvt.Helpers
{
    /// <summary>
    /// ByteHelper
    /// </summary>
    public static class ByteHelper
    {
        /// <summary>
        /// Hex string to byte array
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static byte[] HexToByteArray(string hex)
        {
            hex = hex.Replace("-", string.Empty);

            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        /// <summary>
        /// Byte array to hex string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string ByteArrayToHex(Span<byte> data)
        {
            var hex = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }
    }
}
