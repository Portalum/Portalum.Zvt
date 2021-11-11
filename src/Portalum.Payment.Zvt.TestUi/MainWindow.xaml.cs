using Microsoft.Extensions.Logging;
using Portalum.Payment.Zvt.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
            this.TextBlockStatus.Text = string.Empty;

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

        private void AddOutputElement(OutputInfo outputInfo, Brush backgroundColor)
        {
            this.Output.Dispatcher.Invoke(() =>
            {
                var inlines = new List<Inline>
                {
                    new Bold(new Run(outputInfo.Title)),
                };

                foreach (var line in outputInfo.Lines)
                {
                    inlines.Add(new LineBreak());
                    inlines.Add(new Run(line.TrimEnd()));
                }

                var textBlock = new TextBlock
                {
                    Padding = new Thickness(10),
                    TextWrapping = TextWrapping.Wrap,
                    Width = this.Output.ActualWidth
                };

                textBlock.Inlines.Add(new Run($"{DateTime.Now:HH:mm:ss.fff}") { Foreground = Brushes.DarkGray, FontSize = 9 });
                textBlock.Inlines.Add(new LineBreak());
                textBlock.Inlines.AddRange(inlines);
                textBlock.Measure(new Size(textBlock.Width, 1000));

                var canvas = new Canvas
                {
                    Background = backgroundColor,
                    Height = textBlock.DesiredSize.Height,
                    Margin = new Thickness(10),
                    Effect = new DropShadowEffect
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

        private void ReceiptReceived(ReceiptInfo receiptInfo)
        {
            if (receiptInfo == null)
            {
                return;
            }

            var outputInfo = new OutputInfo
            {
                Title = $"Receipt {receiptInfo.ReceiptType}",
                Lines = new string[]
                {
                    receiptInfo.Content
                }
            };

            try
            {
                this.AddOutputElement(outputInfo, Brushes.White);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
        }

        private void LineReceived(PrintLineInfo printLineInfo)
        {
            this._printLineCache.AppendLine(printLineInfo.Text);

            if (printLineInfo.IsLastLine)
            {
                var outputInfo = new OutputInfo
                {
                    Title = "Lines",
                    Lines = new string[]
                    {
                        this._printLineCache.ToString()
                    }
                };

                this.AddOutputElement(outputInfo, Brushes.White);

                this._printLineCache.Clear();
                return;
            }
        }

        private void StatusInformationReceived(StatusInformation statusInformation)
        {
            this.IntermediateStatusInformationReceived(string.Empty);

            var lines = new List<string>();
            if (statusInformation.TerminalIdentifier > 0)
            {
                lines.Add($"TerminalIdentifier: {statusInformation.TerminalIdentifier}");
            }
            if (!string.IsNullOrEmpty(statusInformation.AdditionalText))
            {
                lines.Add($"AdditionalText: {statusInformation.AdditionalText}");
            }
            if (!string.IsNullOrEmpty(statusInformation.ErrorMessage))
            {
                lines.Add($"ErrorMessage: {statusInformation.ErrorMessage}");
            }
            if (statusInformation.Amount > 0)
            {
                lines.Add($"Amount: {statusInformation.Amount}");
            }
            if (!string.IsNullOrEmpty(statusInformation.CardTechnology))
            {
                lines.Add($"CardTechnology: {statusInformation.CardTechnology}");
            }
            if (!string.IsNullOrEmpty(statusInformation.CardName))
            {
                lines.Add($"CardName: {statusInformation.CardName}");
            }
            if (!string.IsNullOrEmpty(statusInformation.CardholderAuthentication))
            {
                lines.Add($"CardholderAuthentication: {statusInformation.CardholderAuthentication}");
            }
            if (statusInformation.TraceNumber > 0)
            {
                lines.Add($"TraceNumber: {statusInformation.TraceNumber}");
            }
            if (!string.IsNullOrEmpty(statusInformation.VuNumber))
            {
                lines.Add($"VU-Nr.: {statusInformation.VuNumber}");
            }
            if (!string.IsNullOrEmpty(statusInformation.AidAuthorisationAttribute))
            {
                lines.Add($"AidAuthorisationAttribute: {statusInformation.AidAuthorisationAttribute}");
            }
            if (statusInformation.ReceiptNumber > 0)
            {
                lines.Add($"ReceiptNumber: {statusInformation.ReceiptNumber}");
            }
            if (statusInformation.CurrencyCode > 0)
            {
                lines.Add($"CurrencyCode: {statusInformation.CurrencyCode}");
            }

            var outputInfo = new OutputInfo
            {
                Title = "StatusInformation",
                Lines = lines.ToArray()
            };

            this.AddOutputElement(outputInfo, Brushes.LightGoldenrodYellow);
        }

        private void IntermediateStatusInformationReceived(string message)
        {
            this.TextBlockStatus.Dispatcher.Invoke(() =>
            {
                this.TextBlockStatus.Text = message;
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

        private void ConnectionStateChanged(ConnectionState connectionState)
        {
            if (connectionState == ConnectionState.Disconnected)
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
            var commandResponse = await this._zvtClient?.RegistrationAsync(registrationConfig);
            if (commandResponse.State != CommandResponseState.Successful)
            {
                MessageBox.Show($"Command is not successful, current state:{commandResponse.State}");
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
            var commandResponse = await this._zvtClient?.PaymentAsync(amount);
            if (commandResponse.State != CommandResponseState.Successful)
            {
                MessageBox.Show($"Command is not successful, current state:{commandResponse.State}");
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
            var commandResponse = await this._zvtClient?.EndOfDayAsync();
            if (commandResponse.State != CommandResponseState.Successful)
            {
                MessageBox.Show($"Command is not successful, current state:{commandResponse.State}");
            }
            this.ButtonEndOfDay.IsEnabled = true;
        }

        private async Task RepeatLastReceiptAsync()
        {
            if (this._zvtClient == null || !this._deviceCommunication.IsConnected)
            {
                return;
            }

            this.ButtonRepeatReceipt.IsEnabled = false;
            var commandResponse = await this._zvtClient?.RepeatLastReceiptAsync();
            if (commandResponse.State != CommandResponseState.Successful)
            {
                MessageBox.Show($"Command is not successful, current state:{commandResponse.State}");
            }
            this.ButtonRepeatReceipt.IsEnabled = true;
        }

        private async void ButtonRegistration_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RegistrationConfigurationDialog
            {
                Owner = this
            };

            var dialogResult = dialog.ShowDialog();
            if (!dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }

            var registrationConfig = dialog.RegistrationConfig;
            await this.RegistrationAsync(registrationConfig);
        }

        private async void ButtonEndOfDay_Click(object sender, RoutedEventArgs e)
        {
            await this.EndOfDayAsync();
        }

        private async void ButtonRepeatReceipt_Click(object sender, RoutedEventArgs e)
        {
            await this.RepeatLastReceiptAsync();
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
