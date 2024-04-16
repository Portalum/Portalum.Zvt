using System;
using System.Text;
using System.Collections.Generic;
using Portalum.Zvt.Helpers;
using Portalum.Zvt.Parsers;


namespace Portalum.Zvt
{
    public class DisplayTextHelper
    {
        public static byte[] CreateAPDU(int displayDuration, string[] textLines, byte beepTones, byte displayDevice)
        {
            // Start constructing the data block
            List<byte> package = new List<byte>();

            // Display Duration - F0
            package.Add(0xF0);
            package.Add(Convert.ToByte(displayDuration));

            // Add text lines F1-F8
            for (int i = 0; i < 8; i++)
            {
                if (i < textLines.Length && !string.IsNullOrEmpty(textLines[i]))
                {
                    byte field = (byte)(0xF1 + i);
                    byte[] textBytes = Encoding.ASCII.GetBytes(textLines[i]);

                    package.Add(field);
                    package.AddRange(BmpParser.ParseLength(textBytes.Length));
                    package.AddRange(textBytes);
                }
            }

            // Beep Tones - F9
            package.Add(0xF9);
            package.Add(beepTones);

            // Display Device - FD
            package.Add(0xFD);
            package.Add(displayDevice);

            return package.ToArray();
        }
    }
}