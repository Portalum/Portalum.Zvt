using Microsoft.Extensions.Logging;
using Portalum.Zvt.EasyPay.Models;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Portalum.Zvt.EasyPay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly PaymentTerminalConfig _paymentTerminalConfig;

        private readonly ILogger _logger;

        public MainWindow(
            ILoggerFactory loggerFactory,
            PaymentTerminalConfig paymentTerminalConfig,
            decimal amount)
        {
            this._loggerFactory = loggerFactory;
            this._paymentTerminalConfig = paymentTerminalConfig;

            this._logger = loggerFactory.CreateLogger<MainWindow>();

            this.InitializeComponent();
            this.LabelAmount.Content = $"{amount:C2}";

            this.UpdateStatus("Preparing...", StatusType.Information);

            _ = Task.Run(async () => await this.StartPaymentAsync(amount));
        }

        private void UpdateStatus(string status, StatusType statusType)
        {
            this.LabelStatus.Dispatcher.Invoke(() =>
            {
                var brushForeground = Brushes.White;
                var brushBackground = Brushes.Transparent;

                if (statusType == StatusType.Error)
                {
                    brushForeground = new SolidColorBrush(Color.FromRgb(255, 21, 21));
                    brushBackground = Brushes.White;
                }

                this.LabelStatus.Foreground = brushForeground;
                this.LabelStatus.Background = brushBackground;


                this.LabelStatus.Content = status;
            });
        }

        private async Task StartPaymentAsync(decimal amount)
        {
            this._logger.LogInformation($"{nameof(StartPaymentAsync)} - Start");

            var zvtClientConfig = new ZvtClientConfig
            {
                Encoding = ZvtEncoding.CodePage437,
                Language = Zvt.Language.German,
                Password = 000000
            };

            var deviceCommunicationLogger = this._loggerFactory.CreateLogger<TcpNetworkDeviceCommunication>();
            var zvtClientLogger = this._loggerFactory.CreateLogger<ZvtClient>();

            using var deviceCommunication = new TcpNetworkDeviceCommunication(
                this._paymentTerminalConfig.IpAddress,
                port: this._paymentTerminalConfig.Port,
                enableKeepAlive: false,
                logger: deviceCommunicationLogger);

            this.UpdateStatus("Connect to payment terminal...", StatusType.Information);

            if (!await deviceCommunication.ConnectAsync())
            {
                this.UpdateStatus("Cannot connect to payment terminal", StatusType.Error);
                await Task.Delay(3000);

                this._logger.LogError($"{nameof(StartPaymentAsync)} - Cannot connect to {this._paymentTerminalConfig.IpAddress}:{this._paymentTerminalConfig.Port}");
                Application.Current.Dispatcher.Invoke(() => { Application.Current.Shutdown(-2); });
                return;
            }

            var zvtClient = new ZvtClient(deviceCommunication, logger: zvtClientLogger, clientConfig: zvtClientConfig);
            try
            {
                zvtClient.IntermediateStatusInformationReceived += this.IntermediateStatusInformationReceived;

                var response = await zvtClient.PaymentAsync(amount);
                if (response.State == CommandResponseState.Successful)
                {
                    this._logger.LogInformation($"{nameof(StartPaymentAsync)} - Successful");

                    this.UpdateStatus("Payment successful", StatusType.Information);
                    await Task.Delay(1000);

                    Application.Current.Dispatcher.Invoke(() => { Application.Current.Shutdown(0); });
                    return;
                }

                this._logger.LogInformation($"{nameof(StartPaymentAsync)} - Not successful");

                this.UpdateStatus("Payment not successful", StatusType.Error);
                await Task.Delay(1000);

                Application.Current.Dispatcher.Invoke(() => { Application.Current.Shutdown(-3); });
            }
            finally
            {
                zvtClient.IntermediateStatusInformationReceived -= this.IntermediateStatusInformationReceived;
                zvtClient.Dispose();
            }
        }

        private void IntermediateStatusInformationReceived(string status)
        {
            this.UpdateStatus(status, StatusType.Information);
        }
    }
}
