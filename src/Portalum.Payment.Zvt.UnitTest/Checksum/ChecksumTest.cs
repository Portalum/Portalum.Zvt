using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nito.HashAlgorithms;
using Portalum.Payment.Zvt.Helpers;
using System;
using System.Diagnostics;
using System.Linq;

namespace Portalum.Payment.Zvt.UnitTest
{
    [TestClass]
    public class ChecksumTest
    {
        private byte[] GetByteArray(int size, Random rnd)
        {
            byte[] b = new byte[size]; // convert kb to byte
            rnd.NextBytes(b);
            return b;
        }

        [TestMethod]
        public void CalcCrc2_TestRandomPerformance_Successful()
        {
            Random rnd = new Random();
            ChecksumWithLookup.CreateLookupTable();

            for (var i = 0; i < 1000; i++)
            {
                var data = GetByteArray(i, rnd);

                var sw = new Stopwatch();

                sw.Restart();
                var checksum = ChecksumHelper.CalcCrc2(data);
                var expected = new byte[] { (byte)(checksum >> 8), (byte)(checksum & 0xFF) };
                sw.Stop();
                Debug.WriteLine($"A - {sw.Elapsed.TotalMilliseconds}ms");

                //Current Checksum calculator add automatic 0x03 byte on the end

                var temp = data.ToList();
                temp.Add(0x03);
                var data2 = temp.ToArray();

                sw.Restart();

                var calculator = new CRC16(CRC16.Definition.Ccitt);
                var crc16Checksum = calculator.ComputeHash(data2);
                sw.Stop();
                Debug.WriteLine($"B - {sw.Elapsed.TotalMilliseconds}ms");

                Assert.IsTrue(expected.SequenceEqual(crc16Checksum));

                sw.Restart();
                var easyChecksum1 = ChecksumWithLookup.ComputeHash(data2);
                sw.Stop();
                Debug.WriteLine($"C - {sw.Elapsed.TotalMilliseconds}ms");

                Assert.IsTrue(expected.SequenceEqual(easyChecksum1));
            }
        }
    }
}
