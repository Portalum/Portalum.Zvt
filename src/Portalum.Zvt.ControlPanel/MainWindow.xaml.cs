﻿using Microsoft.Extensions.Logging;
using Portalum.Zvt.ControlPanel.Dialogs;
using Portalum.Zvt.ControlPanel.Models;
using Portalum.Zvt.Helpers;
using Portalum.Zvt.Models;
using Portalum.Zvt.Repositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Portalum.Zvt.ControlPanel
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
        private readonly StringBuilder _printLineCache;
        private readonly DeviceConfiguration _deviceConfiguration;
        private CancellationTokenSource _cancellationTokenSource;
        public IEnumerable<CurrencyCodeIso4217> CurrencyCodes => Enum.GetValues<CurrencyCodeIso4217>().AsEnumerable();

        public MainWindow(DeviceConfiguration deviceConfiguration)
        {
            this._deviceConfiguration = deviceConfiguration;

            this._loggerFactory = LoggerFactory.Create(builder =>
                builder.AddFile("default.log", LogLevel.Debug, outputTemplate: "{Timestamp:HH:mm:ss.fff} {Level:u3} {SourceContext} {Message:lj}{NewLine}{Exception}").SetMinimumLevel(LogLevel.Debug));

            this.InitializeComponent();
            this.TextBlockStatus.Text = string.Empty;

            this._printLineCache = new StringBuilder();
            this.ButtonDisconnect.IsEnabled = false;

            this.TextBoxAmount.Text = $"{this.CreateRandomAmount()}";

            this.Currency.ItemsSource = this.CurrencyCodes;
            this.Currency.SelectedItem = CurrencyCodeIso4217.EUR;

            CodePagesEncodingProvider.Instance.GetEncoding(437);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _ = Task.Run(async () => await this.ConnectAsync()).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    throw task.Exception;
                }
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this._deviceCommunication is IDisposable disposable)
            {
                disposable.Dispose();
            }

            this._cancellationTokenSource?.Cancel();
            this._cancellationTokenSource?.Dispose();

            this._zvtClient?.Dispose();
        }

        /// <summary>
        /// Create a random amount from 1 to 5
        /// </summary>
        /// <returns></returns>
        private double CreateRandomAmount()
        {
            var random = new Random();
            var randomValue = random.Next(100, 500);
            return randomValue / 100.0;
        }

        private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            await this.ConnectAsync();
        }

        private async void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (!await this.DisconnectAsync())
            {
                MessageBox.Show("Cannot disconnect");
                return;
            }
        }

        private async Task<bool> ConnectAsync()
        {
            this.ButtonConnect.Dispatcher.Invoke(() =>
            {
                this.ButtonConnect.IsEnabled = false;
            });

            var loggerCommunication = this._loggerFactory.CreateLogger<TcpNetworkDeviceCommunication>();
            var loggerZvtClient = this._loggerFactory.CreateLogger<ZvtClient>();

            this._deviceCommunication = new TcpNetworkDeviceCommunication(
                ipAddress: this._deviceConfiguration.IpAddress,
                port:this._deviceConfiguration.Port,
                enableKeepAlive: this._deviceConfiguration.TcpKeepalive,
                logger: loggerCommunication);

            this._deviceCommunication.ConnectionStateChanged += this.ConnectionStateChanged;

            this.CommunicationUserControl.SetDeviceCommunication(this._deviceCommunication);

            this.SetConnectionInfo($"Try connect {this._deviceCommunication.ConnectionIdentifier}", Colors.White, Colors.Yellow);

            await Task.Delay(50);

            if (!await this._deviceCommunication.ConnectAsync())
            {
                this.SetConnectionInfo("Cannot connect", Colors.White, Colors.OrangeRed);

                this.ButtonConnect.Dispatcher.Invoke(() =>
                {
                    this.ButtonConnect.IsEnabled = true;
                });

                return false;
            }

            this.ButtonDisconnect.Dispatcher.Invoke(() =>
            {
                this.ButtonDisconnect.IsEnabled = true;
            });

            #region Default ZVT

            var zvtClientConfig = new ZvtClientConfig
            {
                Encoding = this._deviceConfiguration.Encoding,
                Language = this._deviceConfiguration.Language
            };

            this._zvtClient = new ZvtClient(
                deviceCommunication: this._deviceCommunication,
                logger: loggerZvtClient,
                clientConfig: zvtClientConfig);

            #endregion

            #region Ingenico ZVT

            //var receiveHandler = new ReceiveHandler(
            //    logger: loggerZvtClient,
            //    encoding: EncodingHelper.GetEncoding(this._deviceConfiguration.Encoding),
            //    errorMessageRepository: new EnglishErrorMessageRepository(),
            //    intermediateStatusRepository: new IngenicoEnglishIntermediateStatusRepository()
            //    );

            //this._zvtClient = new ZvtClient(
            //    deviceCommunication: this._deviceCommunication,
            //    logger: loggerZvtClient,
            //    receiveHandler: receiveHandler);

            #endregion


            this._zvtClient.LineReceived += this.LineReceived;
            this._zvtClient.ReceiptReceived += this.ReceiptReceived;
            this._zvtClient.StatusInformationReceived += this.StatusInformationReceived;
            this._zvtClient.IntermediateStatusInformationReceived += this.IntermediateStatusInformationReceived;

            this.SetConnectionInfo("Connected", Colors.White, Colors.GreenYellow);

            this._cancellationTokenSource?.Dispose();
            this._cancellationTokenSource = new CancellationTokenSource();

            return true;
        }

        private async Task<bool> DisconnectAsync()
        {
            this._cancellationTokenSource?.Cancel();

            if (!await this._deviceCommunication.DisconnectAsync())
            {
                return false;
            }

            this._deviceCommunication.ConnectionStateChanged -= this.ConnectionStateChanged;

            if (this._deviceCommunication is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (this._zvtClient != null)
            {
                this._zvtClient.LineReceived -= this.LineReceived;
                this._zvtClient.ReceiptReceived -= this.ReceiptReceived;
                this._zvtClient.StatusInformationReceived -= this.StatusInformationReceived;
                this._zvtClient.IntermediateStatusInformationReceived -= this.IntermediateStatusInformationReceived;
            }
            this._zvtClient?.Dispose();

            this.ButtonDisconnect.Dispatcher.Invoke(() =>
            {
                this.ButtonDisconnect.IsEnabled = false;
            });
            this.ButtonConnect.Dispatcher.Invoke(() =>
            {
                this.ButtonConnect.IsEnabled = true;
            });

            this.SetConnectionInfo("Disconnected", Colors.White, Colors.Transparent);

            return true;
        }

        private void SetConnectionInfo(string text, Color textColor, Color backgroundColor)
        {
            this.LabelConnectionStatus.Dispatcher.Invoke(() =>
            {
                this.LabelConnectionStatus.Content = text;
                this.LabelConnectionStatus.Foreground = new SolidColorBrush(textColor);
                this.LabelConnectionStatus.BorderBrush = new SolidColorBrush(backgroundColor);
                this.LabelConnectionStatus.Background = new SolidColorBrush(backgroundColor) { Opacity = 0.7 };
            });
        }

        private void AddOutputElement(
            OutputInfo outputInfo,
            Brush backgroundColor,
            double? width = default,
            bool useMonospacedFont = false)
        {
            this.Output.Dispatcher.Invoke(() =>
            {
                var monospacedFont = new FontFamily("Courier New");

                var boxSize = width ?? this.Output.ActualWidth;

                #region Create TextBlock

                var textBlock = new TextBlock
                {
                    Padding = new Thickness(10),
                    TextWrapping = TextWrapping.Wrap,
                    Width = boxSize
                };

                textBlock.Inlines.Add(new Run($"{DateTime.Now:HH:mm:ss.fff}") { Foreground = Brushes.DarkGray, FontSize = 9 });
                textBlock.Inlines.Add(new LineBreak());

                var inlines = new List<Inline>
                {
                    new Bold(new Run(outputInfo.Title))
                };

                if (outputInfo.Lines != null && outputInfo.Lines.Length > 0)
                {
                    inlines.Add(new LineBreak());

                    foreach (var line in outputInfo.Lines)
                    {
                        var textRun = new Run(line.TrimEnd());
                        if (useMonospacedFont)
                        {
                            textRun.FontFamily = monospacedFont;
                        }

                        inlines.Add(new LineBreak());
                        inlines.Add(textRun);
                    }
                }

                textBlock.Inlines.AddRange(inlines);

                textBlock.Measure(new Size(textBlock.Width, 1000));

                #endregion

                #region Draw InfoBox

                var shadow = new DropShadowEffect
                {
                    Color = Colors.DimGray,
                    BlurRadius = 10,
                    Direction = 0,
                    ShadowDepth = 0.5,
                    Opacity = 0.2
                };

                var canvasHeader = new Canvas
                {
                    Background = backgroundColor,
                    Height = 3,
                    Width = width ?? double.NaN,
                    Margin = new Thickness(10, 10, 10, 0),
                    Effect = shadow
                };

                var canvasContent = new Canvas
                {
                    Background = Brushes.White,
                    Height = textBlock.DesiredSize.Height,
                    Width = width ?? double.NaN,
                    Margin = new Thickness(10, 0, 10, 10),
                    Effect = shadow
                };

                #endregion

                canvasContent.Children.Add(textBlock);

                Grid.SetRow(canvasHeader, this._outputRowNumber++);              

                var rowDefinitionHeader = new RowDefinition
                {
                    Height = new GridLength(0, GridUnitType.Star)
                };

                this.Output.RowDefinitions.Add(rowDefinitionHeader);
                this.Output.Children.Add(canvasHeader);

                Grid.SetRow(canvasContent, this._outputRowNumber++);

                var rowDefinition = new RowDefinition
                {
                    Height = new GridLength(0, GridUnitType.Star)
                };
                this.Output.RowDefinitions.Add(rowDefinition);
                this.Output.Children.Add(canvasContent);
                
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
                var receiptWidth = 230;
                this.AddOutputElement(outputInfo, Brushes.White, receiptWidth, useMonospacedFont: true);
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

                var receiptWidth = 230;
                this.AddOutputElement(outputInfo, Brushes.White, receiptWidth, useMonospacedFont: true);

                this._printLineCache.Clear();
                return;
            }
        }

        private void StatusInformationReceived(StatusInformation statusInformation)
        {
            if (statusInformation == null)
            {
                return;
            }

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
            if (!string.IsNullOrEmpty(statusInformation.CardNumber))
            {
                lines.Add($"CardNumber: {statusInformation.CardNumber}");
            }
            if (statusInformation.CardSequenceNumber > 0)
            {
                lines.Add($"CardSequenceNumber: {statusInformation.CardSequenceNumber}");
            }
            if (!string.IsNullOrEmpty(statusInformation.CardholderAuthentication))
            {
                lines.Add($"CardholderAuthentication: {statusInformation.CardholderAuthentication}");
            }
            if (statusInformation.PrintoutNeeded)
            {
                lines.Add($"PrintoutNeeded: {statusInformation.PrintoutNeeded}");
            }
            if (statusInformation.TraceNumber > 0)
            {
                lines.Add($"TraceNumber: {statusInformation.TraceNumber}");
            }
            if (statusInformation.TraceNumberLongFormat > 0)
            {
                lines.Add($"TraceNumberLongFormat: {statusInformation.TraceNumberLongFormat}");
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
            if (statusInformation.DateDay > 0 && statusInformation.DateMonth > 0)
            {
                lines.Add($"Date: {statusInformation.DateDay:00}/{statusInformation.DateMonth:00}");
            }
            if (statusInformation.Time != TimeSpan.Zero)
            {
                lines.Add($"Time: {statusInformation.Time}");
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

        private void ConnectionStateChanged(ConnectionState connectionState)
        {
            if (connectionState == ConnectionState.Disconnected)
            {
                this.DisconnectAsync().GetAwaiter().GetResult();
            }
        }

        private void AddCommandInfo(string command)
        {
            this.AddOutputElement(new OutputInfo
            {
                Title = $"{command} sent"
            }, Brushes.LightSkyBlue);
        }

        private void ProcessCommandRespone(CommandResponse commandResponse)
        {
            switch (commandResponse.State)
            {
                case CommandResponseState.Successful:
                    this.AddOutputElement(new OutputInfo
                    {
                        Title = "Successful"
                    }, Brushes.LightGreen);
                    break;
                case CommandResponseState.Unknown:
                    this.AddOutputElement(new OutputInfo
                    {
                        Title = "Unkown Command",
                        Lines = new[] { $"State: {commandResponse.State}" }
                    }, Brushes.Orange);
                    break;
                case CommandResponseState.Abort:
                case CommandResponseState.Timeout:
                    this.AddOutputElement(new OutputInfo
                    {
                        Title = "Command is not successful",
                        Lines = new[] { $"{commandResponse.State}\r\n\r\n{commandResponse.ErrorMessage}" }
                    }, Brushes.Yellow);
                    break;
                case CommandResponseState.NotSupported:
                case CommandResponseState.Error:
                    this.AddOutputElement(new OutputInfo
                    {
                        Title = "Command is not successful",
                        Lines = new[] { $"{commandResponse.State}\r\n\r\n{commandResponse.ErrorMessage}" }
                    }, Brushes.IndianRed);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private bool IsZvtClientReady()
        {
            if (this._zvtClient == null || !this._deviceCommunication.IsConnected)
            {
                this.AddOutputElement(new OutputInfo
                {
                    Title = "ZVT Client not ready",
                    Lines = new [] { "Check if the connection is active" }
                }, Brushes.Red);

                return false;
            }

            return true;
        }

        private async Task RegistrationAsync(RegistrationConfig registrationConfig)
        {
            if (!this.IsZvtClientReady())
            {
                return;
            }

            this.AddCommandInfo("Registration (06 00)");

            this.ButtonRegistration.IsEnabled = false;
            var commandResponse = await this._zvtClient?.RegistrationAsync(registrationConfig, this._cancellationTokenSource.Token);
            this.ProcessCommandRespone(commandResponse);
            this.ButtonRegistration.IsEnabled = true;
        }

        private async Task PaymentAsync(decimal amount)
        {
            if (!this.IsZvtClientReady())
            {
                return;
            }

            this.AddCommandInfo("Payment/Authorization (06 01)");

            this.ButtonPay.IsEnabled = false;
            var commandResponse = await this._zvtClient?.PaymentAsync(amount, (CurrencyCodeIso4217)this.Currency.SelectedItem, this._cancellationTokenSource.Token);
            this.ProcessCommandRespone(commandResponse);
            this.ButtonPay.IsEnabled = true;
        }

        private async Task EndOfDayAsync()
        {
            if (!this.IsZvtClientReady())
            {
                return;
            }

            this.AddCommandInfo("EndOfDay (06 50)");

            this.ButtonEndOfDay.IsEnabled = false;
            var commandResponse = await this._zvtClient?.EndOfDayAsync(this._cancellationTokenSource.Token);
            this.ProcessCommandRespone(commandResponse);
            this.ButtonEndOfDay.IsEnabled = true;
        }

        private async Task RepeatLastReceiptAsync()
        {
            if (!this.IsZvtClientReady())
            {
                return;
            }

            this.AddCommandInfo("RepeatLastReceiptAsync (06 20)");

            this.ButtonRepeatReceipt.IsEnabled = false;
            var commandResponse = await this._zvtClient?.RepeatLastReceiptAsync(this._cancellationTokenSource.Token);
            this.ProcessCommandRespone(commandResponse);
            this.ButtonRepeatReceipt.IsEnabled = true;
        }

        private async Task RefundAsync(decimal amount)
        {
            if (!this.IsZvtClientReady())
            {
                return;
            }

            this.AddCommandInfo("Refund (06 31)");

            this.ButtonRefund.IsEnabled = false;
            var commandResponse = await this._zvtClient?.RefundAsync(amount, this._cancellationTokenSource.Token);
            this.ProcessCommandRespone(commandResponse);
            this.ButtonRefund.IsEnabled = true;
        }

        private async Task ReversalAsync(int receiptNumber)
        {
            if (!this.IsZvtClientReady())
            {
                return;
            }

            this.AddCommandInfo("Reversal (06 30)");

            this.ButtonReversal.IsEnabled = false;
            var commandResponse = await this._zvtClient?.ReversalAsync(receiptNumber, this._cancellationTokenSource.Token);
            this.ProcessCommandRespone(commandResponse);
            this.ButtonReversal.IsEnabled = true;
        }

        private async Task LogOffAsync()
        {
            if (!this.IsZvtClientReady())
            {
                return;
            }

            this.AddCommandInfo("LogOff (06 02)");

            this.ButtonLogOff.IsEnabled = false;
            var commandResponse = await this._zvtClient.LogOffAsync(this._cancellationTokenSource.Token);
            this.ProcessCommandRespone(commandResponse);
            this.ButtonLogOff.IsEnabled = true;
        }

        private async Task DiagnosisAsync()
        {
            if (!this.IsZvtClientReady())
            {
                return;
            }

            this.AddCommandInfo("Diagnosis (06 70)");

            this.ButtonDiagnosis.IsEnabled = false;
            var commandResponse = await this._zvtClient.DiagnosisAsync(this._cancellationTokenSource.Token);
            this.ProcessCommandRespone(commandResponse);
            this.ButtonDiagnosis.IsEnabled = true;
        }

        private async Task AbortAsync()
        {
            if (!this.IsZvtClientReady())
            {
                return;
            }

            this.AddCommandInfo("Abort (06 B0)");

            this.ButtonDiagnosis.IsEnabled = false;
            var commandResponse = await this._zvtClient.AbortAsync(this._cancellationTokenSource.Token);
            this.ProcessCommandRespone(commandResponse);
            this.ButtonDiagnosis.IsEnabled = true;
        }

        private async Task SoftwareUpdateAsync()
        {
            if (!this.IsZvtClientReady())
            {
                return;
            }

            this.AddCommandInfo("SoftwareUpdate (08 10)");

            this.ButtonSoftwareUpdate.IsEnabled = false;
            var commandResponse = await this._zvtClient.SoftwareUpdateAsync(this._cancellationTokenSource.Token);
            this.ProcessCommandRespone(commandResponse);
            this.ButtonSoftwareUpdate.IsEnabled = true;
        }

        private async Task PrintSystemConfigurationAsync()
        {
            if (!this.IsZvtClientReady())
            {
                return;
            }

            this.AddCommandInfo("PrintSystemConfiguration (06 1A)");

            this.ButtonPrintSystemConfiguration.IsEnabled = false;
            var commandResponse = await this._zvtClient.PrintSystemConfigurationAsync(this._cancellationTokenSource.Token);
            this.ProcessCommandRespone(commandResponse);
            this.ButtonPrintSystemConfiguration.IsEnabled = true;
        }

        private async Task SelftestAsync()
        {
            if (!this.IsZvtClientReady())
            {
                return;
            }

            this.AddCommandInfo("Selftest (06 79)");

            this.ButtonSelftest.IsEnabled = false;
            var commandResponse = await this._zvtClient.SelftestAsync(this._cancellationTokenSource.Token);
            this.ProcessCommandRespone(commandResponse);
            this.ButtonSelftest.IsEnabled = true;
        }

        private async Task StatusEnquiryAsync()
        {
            if (!this.IsZvtClientReady())
            {
                return;
            }

            this.AddCommandInfo("Status-Enquiry (05 01)");

            this.ButtonStatusEnquiry.IsEnabled = false;
            var commandResponse = await this._zvtClient.StatusEnquiryAsync(this._cancellationTokenSource.Token);
            this.ProcessCommandRespone(commandResponse);
            this.ButtonStatusEnquiry.IsEnabled = true;
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
            if (!decimal.TryParse(this.TextBoxAmount.Text.Replace(',', '.'), NumberStyles.Currency, CultureInfo.InvariantCulture, out var amount))
            {
                MessageBox.Show("Cannot parse amount");
                return;
            }

            await this.PaymentAsync(amount);
        }

        private async void ButtonRefund_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(this.TextBoxAmount.Text.Replace(',', '.'), NumberStyles.Currency, CultureInfo.InvariantCulture, out var amount))
            {
                MessageBox.Show("Cannot parse amount");
                return;
            }

            await this.RefundAsync(amount);
        }

        private async  void ButtonReversal_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(this.TextBoxReceiptNumber.Text, out var receiptNumber))
            {
                return;
            }

            await this.ReversalAsync(receiptNumber);
        }

        private async void ButtonLogOff_Click(object sender, RoutedEventArgs e)
        {
            await this.LogOffAsync();
        }

        private async void ButtonDiagnosis_Click(object sender, RoutedEventArgs e)
        {
            await this.DiagnosisAsync();
        }

        private async void ButtonAbort_Click(object sender, RoutedEventArgs e)
        {
            await this.AbortAsync();
        }

        private async void ButtonSoftwareUpdate_Click(object sender, RoutedEventArgs e)
        {
            await this.SoftwareUpdateAsync();
        }

        private async void ButtonPrintSystemConfiguration_Click(object sender, RoutedEventArgs e)
        {
            await this.PrintSystemConfigurationAsync();
        }

        private async void ButtonSelftest_Click(object sender, RoutedEventArgs e)
        {
            await this.SelftestAsync();
        }

        private async void ButtonStatusEnquiry_Click(object sender, RoutedEventArgs e)
        {
            await this.StatusEnquiryAsync();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
