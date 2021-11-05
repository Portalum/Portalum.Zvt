using Microsoft.Extensions.Logging;
using Portalum.Payment.Zvt.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Portalum.Payment.Zvt.TestUi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILoggerFactory _loggerFactory;
        private IDeviceCommunication _deviceCommunication;
        private ZvtClient _zvtClient;
        private int _outputRowNumber = 0;
        private StringBuilder _printLineCache;

        public MainWindow()
        {
            this._loggerFactory = LoggerFactory.Create(builder =>
                builder.AddFile("default.log", LogLevel.Debug, outputTemplate: "{Timestamp:HH:mm:ss.fff} {Level:u3} {SourceContext} {Message:lj}{NewLine}{Exception}").SetMinimumLevel(LogLevel.Debug));

            this.InitializeComponent();
            this.LabelStatus.Content = string.Empty;

            this._printLineCache = new StringBuilder();
            this.TextBoxIpAddress.Background = Brushes.White;
            this.ButtonDisconnect.IsEnabled = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this._deviceCommunication is IDisposable disposable)
            {
                disposable.Dispose();
            }

            this._zvtClient?.Dispose();
        }

        private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            var loggerCommunication = this._loggerFactory.CreateLogger<TcpNetworkDeviceCommunication>();
            var loggerZvtClient = this._loggerFactory.CreateLogger<ZvtClient>();

            this.TextBoxIpAddress.Background = Brushes.White;

            var ipAddress = this.TextBoxIpAddress.Text;
            this._deviceCommunication = new TcpNetworkDeviceCommunication(ipAddress, logger: loggerCommunication);
            this._deviceCommunication.ConnectionStateChanged += ConnectionStateChanged;

            this.CommunicationUserControl.SetDeviceCommunication(this._deviceCommunication);

            if (!await this._deviceCommunication.ConnectAsync())
            {
                this.TextBoxIpAddress.Background = Brushes.OrangeRed;
                MessageBox.Show("Cannot connect");
                return;
            }

            this.TextBoxIpAddress.Background = Brushes.GreenYellow;
            this.ButtonDisconnect.IsEnabled = true;
            this.ButtonConnect.IsEnabled = false;

            this._zvtClient = new ZvtClient(this._deviceCommunication, logger: loggerZvtClient, language: Zvt.Language.German);
            this._zvtClient.LineReceived += this.LineReceived;
            this._zvtClient.ReceiptReceived += this.ReceiptReceived;
            this._zvtClient.StatusInformationReceived += this.StatusInformationReceived;
            this._zvtClient.IntermediateStatusInformationReceived += this.IntermediateStatusInformationReceived;
        }

        private void AddOutputElement(Inline[] inlines, Brush backgroundColor)
        {
            this.Output.Dispatcher.Invoke(() =>
            {
                var textBlock = new TextBlock
                {
                    Padding = new Thickness(10),
                    TextWrapping = TextWrapping.Wrap,
                    Width = 200
                };

                textBlock.Inlines.Add(new Run($"{DateTime.Now}") { Foreground = Brushes.DarkGray, FontSize = 9 });
                textBlock.Inlines.Add(new LineBreak());
                textBlock.Inlines.AddRange(inlines);
                textBlock.Measure(new Size(50, 1000));

                var canvas = new Canvas
                {
                    Background = backgroundColor,
                    Height = textBlock.DesiredSize.Height,
                    Margin = new Thickness(10),
                    Effect = new DropShadowEffect()
                    {
                        Color = Colors.DimGray,
                        BlurRadius = 10,
                        Direction = 0,
                        ShadowDepth = 0.5,
                        Opacity = 0.2
                    }
                };

                canvas.Children.Add(textBlock);
                Grid.SetRow(canvas, this._outputRowNumber++);

                var rowDefinition = new RowDefinition
                {
                    Height = new GridLength(0, GridUnitType.Star)
                };

                this.Output.RowDefinitions.Add(rowDefinition);
                this.Output.Children.Add(canvas);
            });

            this.OutputScrollViewer.Dispatcher.Invoke(() =>
            {
                this.OutputScrollViewer.ScrollToEnd();
            });
        }

        private void ReceiptReceived(ReceiptInfo receipt)
        {
            if (receipt == null)
            {
                return;
            }

            var inlines = new List<Inline>();
            inlines.Add(new Bold(new Run($"Receipt {receipt.ReceiptType}")));
            inlines.Add(new LineBreak());
            inlines.Add(new Run(receipt.Content));

            this.AddOutputElement(inlines.ToArray(), Brushes.White);
        }

        private void LineReceived(PrintLineInfo printLineInfo)
        {
            if (printLineInfo.IsLastLine)
            {
                var inlines = new List<Inline>();
                inlines.Add(new Bold(new Run("Lines")));
                inlines.Add(new LineBreak());
                inlines.Add(new Run(this._printLineCache.ToString()));

                this.AddOutputElement(inlines.ToArray(), Brushes.White);

                this._printLineCache.Clear();
                return;
            }

            this._printLineCache.AppendLine(printLineInfo.Text);
        }

        private void StatusInformationReceived(StatusInformation statusInformation)
        {
            this.IntermediateStatusInformationReceived(string.Empty);

            var inlines = new List<Inline>();
            inlines.Add(new Bold(new Run("StatusInformation")));
            inlines.Add(new LineBreak());
            inlines.Add(new Run($"AdditionalText: {statusInformation.AdditionalText}"));
            inlines.Add(new LineBreak());
            inlines.Add(new Run($"Amount: {statusInformation.Amount}"));
            inlines.Add(new LineBreak());
            inlines.Add(new Run($"CardholderAuthentication: {statusInformation.CardholderAuthentication}"));
            inlines.Add(new LineBreak());
            inlines.Add(new Run($"CardName: {statusInformation.CardName}"));
            inlines.Add(new LineBreak());
            inlines.Add(new Run($"CardTechnology: {statusInformation.CardTechnology}"));
            inlines.Add(new LineBreak());
            inlines.Add(new Run($"TerminalIdentifier: {statusInformation.TerminalIdentifier}"));
            inlines.Add(new LineBreak());
            inlines.Add(new Run($"TraceNumber: {statusInformation.TraceNumber}"));
            inlines.Add(new LineBreak());
            inlines.Add(new Run($"ErrorMessage: {statusInformation.ErrorMessage}"));

            this.AddOutputElement(inlines.ToArray(), Brushes.LightGoldenrodYellow);
        }

        private void IntermediateStatusInformationReceived(string message)
        {
            this.LabelStatus.Dispatcher.Invoke(() =>
            {
                this.LabelStatus.Content = message;
            });
        }

        private async void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (!await this.DisconnectAsync())
            {
                MessageBox.Show("Cannot disconnect");
                return;
            }
        }

        private async Task<bool> DisconnectAsync()
        {
            if (!await this._deviceCommunication.DisconnectAsync())
            {
                return false;
            }

            this._deviceCommunication.ConnectionStateChanged -= ConnectionStateChanged;

            if (this._deviceCommunication is IDisposable disposable)
            {
                disposable.Dispose();
            }

            this._zvtClient.LineReceived -= this.LineReceived;
            this._zvtClient.ReceiptReceived -= this.ReceiptReceived;
            this._zvtClient.StatusInformationReceived -= this.StatusInformationReceived;
            this._zvtClient.IntermediateStatusInformationReceived -= this.IntermediateStatusInformationReceived;
            this._zvtClient?.Dispose();

            this.Dispatcher.Invoke(() =>
            {
                this.ButtonDisconnect.IsEnabled = false;
                this.ButtonConnect.IsEnabled = true;
                this.TextBoxIpAddress.Background = Brushes.White;
            });

            return true;
        }

        private void ConnectionStateChanged()
        {
            if (!this._deviceCommunication.IsConnected)
            {
                this.DisconnectAsync().GetAwaiter().GetResult();
            }
        }

        private async Task RegistrationAsync(RegistrationConfig registrationConfig)
        {
            if (this._zvtClient == null || !this._deviceCommunication.IsConnected)
            {
                return;
            }

            this.ButtonRegistration.IsEnabled = false;
            if (!await this._zvtClient?.RegistrationAsync(registrationConfig))
            {
                MessageBox.Show("Failure");
            }
            this.ButtonRegistration.IsEnabled = true;
        }

        private async Task PaymentAsync(decimal amount)
        {
            if (this._zvtClient == null || !this._deviceCommunication.IsConnected)
            {
                return;
            }

            this.ButtonPay.IsEnabled = false;
            if (!await this._zvtClient?.PaymentAsync(amount))
            {
                MessageBox.Show("Failure");
            }
            this.ButtonPay.IsEnabled = true;
        }

        private async Task EndOfDayAsync()
        {
            if (this._zvtClient == null || !this._deviceCommunication.IsConnected)
            {
                return;
            }

            this.ButtonEndOfDay.IsEnabled = false;
            if (!await this._zvtClient?.EndOfDayAsync())
            {
                MessageBox.Show("Failure");
            }
            this.ButtonEndOfDay.IsEnabled = true;
        }

        private async void ButtonRegistration_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RegistrationConfigurationDialog();
            dialog.Owner = this;
            dialog.ShowDialog();

            var registrationConfig = dialog.RegistrationConfig;

            await this.RegistrationAsync(registrationConfig);
        }

        private async void ButtonEndOfDay_Click(object sender, RoutedEventArgs e)
        {
            await this.EndOfDayAsync();
        }

        private async void ButtonRepeatReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (this._zvtClient == null || !this._deviceCommunication.IsConnected)
            {
                return;
            }

            await this._zvtClient?.RepeatLastReceiptAsync();
        }

        private async void ButtonPay_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(this.TextBoxAmount.Text, NumberStyles.Currency, CultureInfo.InvariantCulture, out var amount))
            {
                MessageBox.Show("Cannot parse amount");
                return;
            }

            await this.PaymentAsync(amount);
        }

        private async void ButtonRefund_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(this.TextBoxAmount.Text, NumberStyles.Currency, CultureInfo.InvariantCulture, out var amount))
            {
                MessageBox.Show("Cannot parse amount");
                return;
            }

            if (this._zvtClient == null || !this._deviceCommunication.IsConnected)
            {
                return;
            }

            await this._zvtClient?.RefundAsync(amount);
        }

        private async  void ButtonReversal_Click(object sender, RoutedEventArgs e)
        {
            if (this._zvtClient == null || !this._deviceCommunication.IsConnected)
            {
                return;
            }

            if (!int.TryParse(this.TextBoxReceiptNumber.Text, out var receiptNumber))
            {
                return;
            }

            await this._zvtClient?.ReversalAsync(receiptNumber);
        }

        private async void ButtonLogOff_Click(object sender, RoutedEventArgs e)
        {
            if (this._zvtClient == null || !this._deviceCommunication.IsConnected)
            {
                return;
            }

            await this._zvtClient.LogOffAsync();
        }

        private async void ButtonDiagnosis_Click(object sender, RoutedEventArgs e)
        {
            if (this._zvtClient == null || !this._deviceCommunication.IsConnected)
            {
                return;
            }

            await this._zvtClient.DiagnosisAsync();
        }
    }
}
