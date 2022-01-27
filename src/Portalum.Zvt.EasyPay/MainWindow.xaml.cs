using Microsoft.Extensions.Logging;
using Portalum.Zvt.EasyPay.Models;
using System.Threading.Tasks;
using System.Windows;

namespace Portalum.Zvt.EasyPay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly PaymentTerminalConfig _paymentTerminalConfig;

        public MainWindow(
            PaymentTerminalConfig paymentTerminalConfig,
            decimal amount)
        {
            this._paymentTerminalConfig = paymentTerminalConfig;

            this.InitializeComponent();

            this.LabelAmount.Content = $"{amount:C2}";

            _ = Task.Run(async () => await this.StartPaymentAsync(amount));
        }

        private async Task StartPaymentAsync(decimal amount)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole().SetMinimumLevel(LogLevel.Debug);
            });

            var zvtClientConfig = new ZvtClientConfig
            {
                Encoding = ZvtEncoding.CodePage437,
                Language = Zvt.Language.German,
                Password = 000000
            };

            var deviceCommunicationLogger = loggerFactory.CreateLogger<TcpNetworkDeviceCommunication>();
            var zvtClientLogger = loggerFactory.CreateLogger<ZvtClient>();

            using var deviceCommunication = new TcpNetworkDeviceCommunication(
                this._paymentTerminalConfig.IpAddress,
                port: this._paymentTerminalConfig.Port,
                enableKeepAlive: false,
                logger: deviceCommunicationLogger);

            if (!await deviceCommunication.ConnectAsync())
            {
                Application.Current.Dispatcher.Invoke(() => { Application.Current.Shutdown(-2); });
                return;
            }

            using var zvtClient = new ZvtClient(deviceCommunication, logger: zvtClientLogger, clientConfig: zvtClientConfig);

            var response = await zvtClient.PaymentAsync(amount);
            if (response.State == CommandResponseState.Successful)
            {
                Application.Current.Dispatcher.Invoke(() => { Application.Current.Shutdown(0); });
                return;
            }

            Application.Current.Dispatcher.Invoke(() => { Application.Current.Shutdown(-3); });
            return;
        }
    }
}
