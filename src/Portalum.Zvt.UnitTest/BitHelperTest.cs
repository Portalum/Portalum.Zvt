using Microsoft.VisualStudio.TestTools.UnitTesting;
using Portalum.Zvt.Helpers;
using System;

namespace Portalum.Zvt.UnitTest
{
    [TestClass]
    public class BitHelperTest
    {
        [TestMethod]
        public void SetBit_BitIndexTooHigh_Failure()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => BitHelper.SetBit(0xFF, 9));
        }

        [TestMethod]
        public void SetBit_BitIndexTooLow_Failure()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => BitHelper.SetBit(0xFF, -1));
        }
    }
}
