using Portalum.Zvt.TestUi.Dialogs;
using System.Windows;

namespace Portalum.Zvt.TestUi
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void ApplicationStart(object sender, StartupEventArgs e)
        {
            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var dialog = new DeviceConfigurationDialog();
            if (dialog.ShowDialog() == true)
            {
                var deviceConfiguration = dialog.DeviceConfiguration;

                var mainWindow = new MainWindow(deviceConfiguration);
                Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                Current.MainWindow = mainWindow;
                mainWindow.Show();
                return;
            }

            Current.Shutdown(-1);
        }
    }
}
