using System.Text;

namespace Portalum.Zvt.Helpers
{
    /// <summary>
    /// EncodingHelper
    /// </summary>
    public static class EncodingHelper
    {
        /// <summary>
        /// GetEncoding from ZvtEncoding
        /// </summary>
        /// <param name="zvtEncoding"></param>
        /// <returns></returns>
        public static Encoding GetEncoding(ZvtEncoding zvtEncoding)
        {
            switch (zvtEncoding)
            {
                case ZvtEncoding.UTF8:
                    return Encoding.UTF8;
                case ZvtEncoding.ISO_8859_1:
                    return Encoding.GetEncoding("iso-8859-1");
                case ZvtEncoding.ISO_8859_2:
                    return Encoding.GetEncoding("iso-8859-2");
                case ZvtEncoding.ISO_8859_15:
                    return Encoding.GetEncoding("iso-8859-15");
                case ZvtEncoding.CodePage437:
                default:
                    return Encoding.GetEncoding(437);
            }
        }
    }
}
