using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Portalum.Zvt.Helpers
{
    /// <summary>
    /// Package Helper
    /// </summary>
    public static class PackageHelper
    {
        /// <summary>
        /// Create a zvt command package
        /// </summary>
        /// <param name="controlField"></param>
        /// <param name="packageData"></param>
        /// <returns></returns>
        public static byte[] Create(
            byte[] controlField,
            IEnumerable<byte> packageData)
        {
            var data = packageData.ToArray();

            using var memoryStream = new MemoryStream();
            memoryStream.Write(controlField, 0, controlField.Length);
            memoryStream.WriteByte((byte)data.Length);
            memoryStream.Write(data, 0, data.Length);
            return memoryStream.ToArray();
        }
    }
}
