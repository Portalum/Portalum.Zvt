using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO.Ports;
using System.Threading.Tasks;

namespace Portalum.Zvt.UnitTest
{
    [Ignore]
    [TestClass]
    public class SerialTest
    {
        private string _comPort = "COM8";

        [TestMethod]
        public async Task SerialProvider_StartPayment_Successful()
        {
            var loggerDeviceCommunication = LoggerHelper.GetLogger<SerialPortDeviceCommunication>();
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();

            using var communication = new SerialPortDeviceCommunication(this._comPort, 9600, Parity.None, 8, StopBits.One, loggerDeviceCommunication.Object);
            var isConnected = await communication.ConnectAsync();
            Assert.IsTrue(isConnected);

            using var zvtClient = new ZvtClient(communication, loggerZvtClient.Object);
            var commandResponse = await zvtClient.PaymentAsync(12m);
            Assert.IsTrue(commandResponse.State == CommandResponseState.Successful);
        }
    }
}
