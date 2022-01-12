using System;
using System.Windows;

namespace Portalum.Zvt.TestUi
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

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            this.DeviceConfiguration.IpAddress = this.TextBoxIpAddress.Text.Trim();
            this.DeviceConfiguration.Language = (Language)this.ComboBoxLanguage.SelectedItem;
            this.DeviceConfiguration.Encoding = (ZvtEncoding)this.ComboBoxEncoding.SelectedItem;

            this.DialogResult = true;
            this.Close();
        }
    }
}
