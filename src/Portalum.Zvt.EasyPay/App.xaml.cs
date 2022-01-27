using CommandLine;
using Microsoft.Extensions.Configuration;
using Portalum.Zvt.EasyPay.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Portalum.Zvt.EasyPay
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly string _configurationFile = "appsettings.json";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!File.Exists(this._configurationFile))
            {
                Console.WriteLine($"Configuration file not available, {this._configurationFile}");
                Current.Shutdown(-2);
                return;
            }

            Parser.Default.ParseArguments<CommandLineOptions>(e.Args)
                .WithParsed(this.RunOptions)
                .WithNotParsed(this.HandleParseError);
        }

        private PaymentTerminalConfig GetConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(this._configurationFile, optional: false);

            IConfigurationRoot configuration = builder.Build();

            if (!int.TryParse(configuration["Port"], out var port))
            {
                Console.WriteLine("Cannot parse port from configuration file");
            }

            return new PaymentTerminalConfig
            {
                IpAddress = configuration["IpAddress"],
                Port = port
            };
        }

        private void RunOptions(CommandLineOptions options)
        {
            var configuration = this.GetConfiguration();

            var window = new MainWindow(configuration, options.Amount);
            window.Show();
        }

        private void HandleParseError(IEnumerable<Error> errors)
        {
            Current.Shutdown(-1);
        }
    }
}
