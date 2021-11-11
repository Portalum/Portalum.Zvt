using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Portalum.Payment.Zvt.Helpers;
using Portalum.Payment.Zvt.Models;
using Portalum.Payment.Zvt.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Portalum.Payment.Zvt
{
    /// <summary>
    /// ZVT Protocol Client
    /// </summary>
    public class ZvtClient : IDisposable
    {
        //Documentation
        //https://www.terminalhersteller.de/downloads/PA00P016_04_en.pdf
        //https://www.terminalhersteller.de/downloads/PA00P015_13.09_final_en.pdf

        private readonly ILogger<ZvtClient> _logger;
        private readonly byte[] _passwordData;

        private readonly ZvtCommunication _zvtCommunication;
        private readonly IReceiveHandler _receiveHandler;

        public event Action<StatusInformation> StatusInformationReceived;
        public event Action<string> IntermediateStatusInformationReceived;
        public event Action<PrintLineInfo> LineReceived;
        public event Action<ReceiptInfo> ReceiptReceived;

        /// <summary>
        /// ZvtClient
        /// </summary>
        /// <param name="deviceCommunication"></param>
        /// <param name="logger"></param>
        /// <param name="password">The password of the PT device</param>
        /// <param name="receiveHandler">The password of the PT device</param>
        /// <param name="language"></param>
        public ZvtClient(
            IDeviceCommunication deviceCommunication,
            ILogger<ZvtClient> logger = default,
            int password = 000000,
            IReceiveHandler receiveHandler = default,
            Language language = Language.English)
        {
            if (logger == null)
            {
                logger = new NullLogger<ZvtClient>();
            }
            this._logger = logger;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            this._passwordData = NumberHelper.IntToBcd(password);

            #region Language

            IErrorMessageRepository errorMessageRepository;
            IIntermediateStatusRepository intermediateStatusRepository;
            if (language == Language.German)
            {
                errorMessageRepository = new EnglishErrorMessageRepository(); //No Germman translation available
                intermediateStatusRepository = new GermanIntermediateStatusRepository();
            }
            else
            {
                errorMessageRepository = new EnglishErrorMessageRepository();
                intermediateStatusRepository = new EnglishIntermediateStatusRepository();
            }

            #endregion

            #region ReceiveHandler

            if (receiveHandler == default)
            {
                this._receiveHandler = new ReceiveHandler(logger, errorMessageRepository, intermediateStatusRepository);
            }
            else
            {
                this._receiveHandler = receiveHandler;
            }
            
            this._receiveHandler.IntermediateStatusInformationReceived += this.ProcessIntermediateStatusInformationReceived;
            this._receiveHandler.StatusInformationReceived += this.ProcessStatusInformationReceived;
            this._receiveHandler.LineReceived += this.ProcessLineReceived;
            this._receiveHandler.ReceiptReceived += this.ProcessReceiptReceived;

            #endregion

            this._zvtCommunication = new ZvtCommunication(logger, deviceCommunication);
            this._zvtCommunication.DataReceived += this.DataReceived;

        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._zvtCommunication.DataReceived -= this.DataReceived;
                this._zvtCommunication.Dispose();

                this._receiveHandler.IntermediateStatusInformationReceived -= this.ProcessIntermediateStatusInformationReceived;
                this._receiveHandler.StatusInformationReceived -= this.ProcessStatusInformationReceived;
                this._receiveHandler.LineReceived -= this.ProcessLineReceived;
                this._receiveHandler.ReceiptReceived -= this.ProcessReceiptReceived;
            }
        }

        private void ProcessIntermediateStatusInformationReceived(string message)
        {
            this.IntermediateStatusInformationReceived?.Invoke(message);
        }

        private void ProcessStatusInformationReceived(StatusInformation statusInformation)
        {
            this.StatusInformationReceived?.Invoke(statusInformation);
        }

        private void ProcessLineReceived(PrintLineInfo printLineInfo)
        {
            this.LineReceived?.Invoke(printLineInfo);
        }

        private void ProcessReceiptReceived(ReceiptInfo receiptInfo)
        {
            this.ReceiptReceived?.Invoke(receiptInfo);
        }

        private void DataReceived(byte[] data)
        {
            if (!this._receiveHandler.ProcessData(data))
            {
                this._logger.LogError($"{nameof(DataReceived)} - Unprocessable data received {BitConverter.ToString(data)}");
            }
        }

        private async Task<CommandResponseState> SendCommandAsync(byte[] commandData, int commandResultTimeout = 90000)
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            var commandResult = CommandResponseState.Unknown;

            void completionReceived()
            {
                commandResult = CommandResponseState.Successful;
                cancellationTokenSource.Cancel();
            }

            void abortReceived(string errorMessage)
            {
                commandResult = CommandResponseState.Abort;
                cancellationTokenSource.Cancel();
            }

            void notSupportedReceived()
            {
                commandResult = CommandResponseState.NotSupported;
                cancellationTokenSource.Cancel();
            }

            try
            {
                this._receiveHandler.CompletionReceived += completionReceived;
                this._receiveHandler.AbortReceived += abortReceived;
                this._receiveHandler.NotSupportedReceived += notSupportedReceived;

                this._logger.LogDebug($"{nameof(SendCommandAsync)} - Send command to PT");

                if (!await this._zvtCommunication.SendCommandAsync(commandData))
                {
                    this._logger.LogError($"{nameof(SendCommandAsync)} - Failure on send command");
                    return CommandResponseState.Error;
                }

                await Task.Delay(commandResultTimeout, cancellationTokenSource.Token).ContinueWith(task =>
                {
                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        commandResult = CommandResponseState.Timeout;
                        this._logger.LogError($"{nameof(SendCommandAsync)} - No result received in the specified timeout {commandResultTimeout}ms");
                    }
                });
            }
            finally
            {
                this._receiveHandler.NotSupportedReceived -= notSupportedReceived;
                this._receiveHandler.AbortReceived -= abortReceived;
                this._receiveHandler.CompletionReceived -= completionReceived;
            }

            return commandResult;
        }

        private byte[] CreatePackage(byte[] controlField, IEnumerable<byte> packageData)
        {
            var package = new List<byte>();
            package.AddRange(controlField);
            package.Add((byte)packageData.Count());
            package.AddRange(packageData);
            return package.ToArray();
        }

        /// <summary>
        /// Registration (06 00)
        /// Using the command Registration the ECR can set up different configurations on the PT and also control the current status of the PT.
        /// </summary>
        /// <param name="registrationConfig"></param>
        /// <returns></returns>
        public async Task<CommandResponse> RegistrationAsync(RegistrationConfig registrationConfig)
        {
            this._logger.LogInformation($"{nameof(RegistrationAsync)} - Execute");

            _ = registrationConfig ?? throw new ArgumentNullException(nameof(registrationConfig));

            var configByte = registrationConfig.GetConfigByte();
            var serviceByte = registrationConfig.GetServiceByte();

            var package = new List<byte>();
            package.AddRange(this._passwordData);
            package.Add(configByte);

            //Currency Code (CC)
            //ISO4217 (https://en.wikipedia.org/wiki/ISO_4217)
            var currencyNumericCodeData = NumberHelper.IntToBcd(978, 2); //978 = Euro
            package.AddRange(currencyNumericCodeData);

            //Service byte
            package.Add(0x03); //Service byte indicator
            package.Add(serviceByte);

            if (registrationConfig.ActivateTlvSupport)
            {
                //Add empty TLV Container
                //package.Add(0x06); //TLV
                //package.Add(0x00); //TLV-Length

                //Add TLV Container permit 06D3 (Card complete)
                package.Add(0x06); //TLV
                package.Add(0x06); //tlv legnth
                package.Add(0x26); //list of permitted ZVT-Commands
                package.Add(0x04); //length
                package.Add(0x0A); //ZVT-command
                package.Add(0x02); //length
                package.Add(0x06); //06 first hex of print text block
                package.Add(0xD3); //D3 second hex of print text block

                //TLV TAG
                //10 - Number of columns and number of lines of the merchant-display
                //11 - Number of columns and number of lines of the customer-display
                //12 - Number of characters per line of the printer
                //14 - ISO-Character set
                //1A - Max length the APDU
                //26 - List of permitted ZVT commands
                //27 - List of supported character-sets
                //28 - List of supported languages
                //29 - List of menus which should be displayed over the ECR or on a second customer-display
                //2A - List of menus which the ECR will not display and therefore must be displayed on the PT
                //40 - EMV-configuration-parameter
                //1F04 - Receipt parameter
                //1F05 - Transaction parameter
            }

            var fullPackage = this.CreatePackage(new byte[] { 0x06, 0x00 }, package);
            var responseStatus = await this.SendCommandAsync(fullPackage);

            return new CommandResponse
            {
                State = responseStatus
            };
        }

        /// <summary>
        /// Authorization (06 01)
        /// Payment process and transmits the amount from the ECR to PT.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public async Task<CommandResponse> PaymentAsync(decimal amount)
        {
            this._logger.LogInformation($"{nameof(PaymentAsync)} - Execute with amount of:{amount}");

            var package = new List<byte>();
            package.Add(0x04); //Amount prefix
            package.AddRange(NumberHelper.DecimalToBcd(amount));

            var fullPackage = this.CreatePackage(new byte[] { 0x06, 0x01 }, package);
            var responseStatus = await this.SendCommandAsync(fullPackage);

            return new CommandResponse
            {
                State = responseStatus
            };
        }

        /// <summary>
        /// Reversal (06 30)
        /// This command reverses a payment-procedure and transfers the receipt-number of the transaction to be reversed from the ECR to PT.
        /// The result of the reversal-process is sent to the ECR after Completion of the booking-process.
        /// </summary>
        /// <param name="receiptNumber">four-digit number</param>
        /// <returns></returns>
        public async Task<CommandResponse> ReversalAsync(int receiptNumber)
        {
            this._logger.LogInformation($"{nameof(ReversalAsync)} - Execute");

            var package = new List<byte>();
            package.AddRange(this._passwordData);
            package.Add(0x87); //Receipt-no prefix
            package.AddRange(NumberHelper.IntToBcd(receiptNumber, 2));

            var fullPackage = this.CreatePackage(new byte[] { 0x06, 0x30 }, package);
            var responseStatus = await this.SendCommandAsync(fullPackage);

            return new CommandResponse
            {
                State = responseStatus
            };
        }

        /// <summary>
        /// Refund (06 31)
        /// This command starts a Refund on the PT. The result of the Refund is reported to the ECR after completion of the booking-process.
        /// </summary>
        /// <returns></returns>
        public async Task<CommandResponse> RefundAsync(decimal amount)
        {
            this._logger.LogInformation($"{nameof(RefundAsync)} - Execute");

            var package = new List<byte>();
            package.AddRange(this._passwordData);
            package.Add(0x04); //Amount prefix
            package.AddRange(NumberHelper.DecimalToBcd(amount));

            var fullPackage = this.CreatePackage(new byte[] { 0x06, 0x31 }, package);
            var responseStatus = await this.SendCommandAsync(fullPackage);

            return new CommandResponse
            {
                State = responseStatus
            };
        }

        /// <summary>
        /// End-of-Day (06 50)
        /// ECR induces the PT to transfer the stored turnover to the host.
        /// </summary>
        /// <returns></returns>
        public async Task<CommandResponse> EndOfDayAsync()
        {
            this._logger.LogInformation($"{nameof(EndOfDayAsync)} - Execute");

            var package = new List<byte>();
            package.AddRange(this._passwordData);

            var fullPackage = this.CreatePackage(new byte[] { 0x06, 0x50 }, package);
            var responseStatus = await this.SendCommandAsync(fullPackage);

            return new CommandResponse
            {
                State = responseStatus
            };
        }

        /// <summary>
        /// Send Turnover Totals (06 10)
        /// With this command the ECR causes the PT to send an overview about the stored transactions.
        /// </summary>
        /// <returns></returns>
        public async Task<CommandResponse> SendTurnoverTotalsAsync()
        {
            this._logger.LogInformation($"{nameof(SendTurnoverTotalsAsync)} - Execute");

            var package = new List<byte>();
            package.AddRange(this._passwordData);

            var fullPackage = this.CreatePackage(new byte[] { 0x06, 0x10 }, package);
            var responseStatus = await this.SendCommandAsync(fullPackage);

            return new CommandResponse
            {
                State = responseStatus
            };
        }

        /// <summary>
        /// Repeat Receipt (06 20)
        /// This command serves to repeat printing of the last stored payment-receipts or End-of-Day-receipt.
        /// </summary>
        /// <returns></returns>
        public async Task<CommandResponse> RepeatLastReceiptAsync()
        {
            this._logger.LogInformation($"{nameof(RepeatLastReceiptAsync)} - Execute");

            var package = new List<byte>();
            package.AddRange(this._passwordData);

            var fullPackage = this.CreatePackage(new byte[] { 0x06, 0x20 }, package);
            var responseStatus = await this.SendCommandAsync(fullPackage);

            return new CommandResponse
            {
                State = responseStatus
            };
        }

        /// <summary>
        /// Log-Off (06 02)
        /// </summary>
        /// <returns></returns>
        public async Task<CommandResponse> LogOffAsync()
        {
            this._logger.LogInformation($"{nameof(LogOffAsync)} - Execute");

            var package = new List<byte>();

            var fullPackage = this.CreatePackage(new byte[] { 0x06, 0x02 }, package);
            var responseStatus = await this.SendCommandAsync(fullPackage);

            return new CommandResponse
            {
                State = responseStatus
            };
        }

        /// <summary>
        /// Diagnosis (06 70)
        /// </summary>
        /// <returns></returns>
        public async Task<CommandResponse> DiagnosisAsync()
        {
            this._logger.LogInformation($"{nameof(DiagnosisAsync)} - Execute");

            var package = new List<byte>();

            var fullPackage = this.CreatePackage(new byte[] { 0x06, 0x70 }, package);
            var responseStatus = await this.SendCommandAsync(fullPackage);

            return new CommandResponse
            {
                State = responseStatus
            };
        }
    }
}
