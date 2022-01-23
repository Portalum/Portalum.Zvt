using System.Windows;

namespace Portalum.Zvt.ControlPanel.Dialogs
{
    /// <summary>
    /// Interaction logic for RegistrationDialog.xaml
    /// </summary>
    public partial class RegistrationConfigurationDialog : Window
    {
        public RegistrationConfig RegistrationConfig { get; private set; }

        public RegistrationConfigurationDialog()
        {
            this.InitializeComponent();
            this.UpdateRegistrationConfig();
        }

        private void UpdateRegistrationConfig()
        {
            if (this.CheckBoxAllowAdministrationViaPaymentTerminal == null)
            {
                return;
            }

            if (this.CheckBoxAllowStartPaymentViaPaymentTerminal == null)
            {
                return;
            }

            if (this.CheckBoxReceiptPrintoutForAdministrationFunctionsViaPaymentTerminal == null)
            {
                return;
            }

            if (this.CheckBoxReceiptPrintoutForPaymentFunctionsViaPaymentTerminal == null)
            {
                return;
            }

            if (this.CheckBoxSendIntermediateStatusInformation == null)
            {
                return;
            }

            this.RegistrationConfig = new RegistrationConfig
            {
                AllowAdministrationViaPaymentTerminal = this.CheckBoxAllowAdministrationViaPaymentTerminal.IsChecked.Value,
                AllowStartPaymentViaPaymentTerminal = this.CheckBoxAllowStartPaymentViaPaymentTerminal.IsChecked.Value,
                ReceiptPrintoutForAdministrationFunctionsViaPaymentTerminal = this.CheckBoxReceiptPrintoutForAdministrationFunctionsViaPaymentTerminal.IsChecked.Value,
                ReceiptPrintoutForPaymentFunctionsViaPaymentTerminal = this.CheckBoxReceiptPrintoutForPaymentFunctionsViaPaymentTerminal.IsChecked.Value,
                SendIntermediateStatusInformation = this.CheckBoxSendIntermediateStatusInformation.IsChecked.Value,
                ActivateTlvSupport = this.CheckBoxActivateTlvSupport.IsChecked.Value
            };

            this.SetTitle();
        }

        private void SetTitle()
        {
            var configByte = this.RegistrationConfig.GetConfigByte().ToString("X2");
            this.Title = $"Registration Configuration (ConfigByte:{configByte})";
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            this.UpdateRegistrationConfig();

            this.DialogResult = true;
            this.Close();
        }

        private void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            this.UpdateRegistrationConfig();
        }
    }
}
