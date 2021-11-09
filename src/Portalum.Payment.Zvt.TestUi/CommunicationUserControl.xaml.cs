using System;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows.Controls;

namespace Portalum.Payment.Zvt.TestUi
{
    /// <summary>
    /// Interaction logic for CommunicationUserControl.xaml
    /// </summary>
    public partial class CommunicationUserControl : UserControl
    {
        private IDeviceCommunication _deviceCommunication;

        public CommunicationUserControl()
        {
            this.InitializeComponent();
        }

        public void SetDeviceCommunication(IDeviceCommunication deviceCommunication)
        {
            if (this._deviceCommunication != null)
            {
                this._deviceCommunication.DataReceived -= DataReceived;
            }
            this._deviceCommunication = deviceCommunication;
            this._deviceCommunication.DataReceived += DataReceived;
            this._deviceCommunication.DataSent += DataSent;
        }

        private void DataSent(byte[] data)
        {
            this.DataGridCommunication.Dispatcher.Invoke(() =>
            {
                this.DataGridCommunication.Items.Add(new CommunicationInfo { Timestamp = DateTime.Now, Category = "ECR->PT", HexData = BitConverter.ToString(data) });
            });
        }

        private void DataReceived(byte[] data)
        {
            this.DataGridCommunication.Dispatcher.Invoke(() =>
            {
                this.DataGridCommunication.Items.Add(new CommunicationInfo { Timestamp = DateTime.Now, Category = "PT->ECR", HexData = BitConverter.ToString(data) });
            });
        }

        private async void ButtonSave_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var serializeOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var items = this.DataGridCommunication.Items.OfType<CommunicationInfo>();
            var jsonString = JsonSerializer.Serialize(items, serializeOptions);
            await File.WriteAllTextAsync($"communication-export-{DateTime.Now:yyyy-mm-dd-hh-mm-ss}.json", jsonString);
        }

        private void ButtonClear_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.DataGridCommunication.Items.Clear();
        }
    }
}
