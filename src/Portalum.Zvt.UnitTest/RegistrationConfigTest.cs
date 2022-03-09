using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Portalum.Zvt.UnitTest
{
    [TestClass]
    public class RegistrationConfigTest
    {
        [TestMethod]
        public void GetServiceByte_CheckServiceByte1_Successful()
        {
            var registrationConfig = new RegistrationConfig
            {
                ServiceMenuIsDisabledOnTheFunctionKeyOfThePaymentTerminal = true
            };

            var serviceByte = registrationConfig.GetServiceByte();

            Assert.AreEqual(0x01, serviceByte);
        }

        [TestMethod]
        public void GetServiceByte_CheckServiceByte2_Successful()
        {
            var registrationConfig = new RegistrationConfig
            {
                ServiceMenuIsDisabledOnTheFunctionKeyOfThePaymentTerminal = true,
                TextAtDisplayInCapitalLettersAtThePaymentTerminal = true
            };

            var serviceByte = registrationConfig.GetServiceByte();

            Assert.AreEqual(0x03, serviceByte);
        }
    }
}
