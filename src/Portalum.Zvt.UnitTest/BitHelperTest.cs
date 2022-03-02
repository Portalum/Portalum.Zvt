using Microsoft.VisualStudio.TestTools.UnitTesting;
using Portalum.Zvt.Helpers;
using System;

namespace Portalum.Zvt.UnitTest
{
    [TestClass]
    public class BitHelperTest
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SetBit_BitIndexTooHigh_Failure()
        {
            BitHelper.SetBit(0xFF, 9);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SetBit_BitIndexTooLow_Failure()
        {
            BitHelper.SetBit(0xFF, -1);
        }
    }
}
