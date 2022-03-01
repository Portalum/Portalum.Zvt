using Portalum.Zvt.ControlPanel.Models;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Portalum.Zvt.ControlPanel.Dialogs
{
    /// <summary>
    /// Interaction logic for DeviceConfigurationDialog.xaml
    /// </summary>
    public partial class DeviceConfigurationDialog : Window
    {
        private Regex _regexPortNumber = new Regex("[^0-9]{1,5}");

        public DeviceConfiguration DeviceConfiguration { get; private set; }

        public DeviceConfigurationDialog()
        {
            this.InitializeComponent();

            this.DeviceConfiguration = new DeviceConfiguration();

            this.ComboBoxLanguage.ItemsSource = Enum.GetValues(typeof(Language));
            this.ComboBoxLanguage.SelectedItem = Zvt.Language.English;

            this.ComboBoxEncoding.ItemsSource = Enum.GetValues(typeof(ZvtEncoding));
            this.ComboBoxEncoding.SelectedItem = ZvtEncoding.CodePage437;
        }

        private void CloseDialog()
        {
            if (!int.TryParse(this.TextBoxPort.Text, out var port))
            {
                MessageBox.Show("Cannot parse port", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.DeviceConfiguration.IpAddress = this.TextBoxIpAddress.Text.Trim();
            this.DeviceConfiguration.Port = port;
            this.DeviceConfiguration.Language = (Language)this.ComboBoxLanguage.SelectedItem;
            this.DeviceConfiguration.Encoding = (ZvtEncoding)this.ComboBoxEncoding.SelectedItem;
            this.DeviceConfiguration.TcpKeepalive = (bool)this.CheckBoxTcpKeepalive.IsChecked;

            this.DialogResult = true;
            this.Close();
        }

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            this.CloseDialog();
        }

        private void TextBoxIpAddress_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.CloseDialog();
            }
        }

        private void TextBoxIpAddress_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).SelectAll();
        }

        private void PortValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = this._regexPortNumber.IsMatch(e.Text);
        }
    }
}
