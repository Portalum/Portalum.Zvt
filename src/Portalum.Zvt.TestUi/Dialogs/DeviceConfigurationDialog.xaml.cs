using Portalum.Zvt.TestUi.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Portalum.Zvt.TestUi.Dialogs
{
    /// <summary>
    /// Interaction logic for DeviceConfigurationDialog.xaml
    /// </summary>
    public partial class DeviceConfigurationDialog : Window
    {
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
            this.DeviceConfiguration.IpAddress = this.TextBoxIpAddress.Text.Trim();
            this.DeviceConfiguration.Language = (Language)this.ComboBoxLanguage.SelectedItem;
            this.DeviceConfiguration.Encoding = (ZvtEncoding)this.ComboBoxEncoding.SelectedItem;

            this.DialogResult = true;
            this.Close();
        }

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            this.CloseDialog();
        }

        private void TextBoxIpAddress_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.CloseDialog();
            }
        }

        private void TextBoxIpAddress_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).SelectAll();
        }
    }
}
