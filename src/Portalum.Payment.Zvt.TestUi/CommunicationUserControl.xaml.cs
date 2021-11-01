using System;
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
    }
}
