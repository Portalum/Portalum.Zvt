using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Portalum.Payment.Zvt.UnitTest
{
    [TestClass]
    public class RegistrationConfigTest
    {
        [TestMethod]
        public void GetServiceByte_CheckServiceByte1_Successful()
        {
            var registrationConfig = new RegistrationConfig();
            registrationConfig.ServiceMenuIsDisabledOnTheFunctionKeyOfThePaymentTerminal = true;
            var serviceByte = registrationConfig.GetServiceByte();

            Assert.AreEqual(0x01, serviceByte);
        }

        [TestMethod]
        public void GetServiceByte_CheckServiceByte2_Successful()
        {
            var registrationConfig = new RegistrationConfig();
            registrationConfig.ServiceMenuIsDisabledOnTheFunctionKeyOfThePaymentTerminal = true;
            registrationConfig.TextAtDisplayInCapitalLettersAtThePaymentTerminal = true;
            var serviceByte = registrationConfig.GetServiceByte();

            Assert.AreEqual(0x03, serviceByte);
        }
    }
}
