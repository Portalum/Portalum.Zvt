using System.Windows;

namespace Portalum.Payment.Zvt.TestUi
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
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            this.RegistrationConfig = new RegistrationConfig
            {
                AllowAdministrationViaPaymentTerminal = this.CheckBoxAllowAdministrationViaPaymentTerminal.IsChecked.Value,
                AllowStartPaymentViaPaymentTerminal = this.CheckBoxAllowStartPaymentViaPaymentTerminal.IsChecked.Value,
                ReceiptPrintoutForAdministrationFunctionsViaPaymentTerminal = this.CheckBoxReceiptPrintoutForAdministrationFunctionsViaPaymentTerminal.IsChecked.Value,
                ReceiptPrintoutForPaymentFunctionsViaPaymentTerminal = this.CheckBoxReceiptPrintoutForPaymentFunctionsViaPaymentTerminal.IsChecked.Value,
                SendIntermediateStatusInformation = this.CheckBoxSendIntermediateStatusInformation.IsChecked.Value,
                ActivateTlvSupport = this.CheckBoxActivateTlvSupport.IsChecked.Value
            };

            this.DialogResult = true;
            this.Close();
        }
    }
}
