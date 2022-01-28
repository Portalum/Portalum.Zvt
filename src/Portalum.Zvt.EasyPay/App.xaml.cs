using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Portalum.Zvt.EasyPay.Models;
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
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public App()
        {
            this._loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddFile("default.log", LogLevel.Debug, outputTemplate: "{Timestamp:HH:mm:ss.fff} {Level:u3} {SourceContext} {Message:lj}{NewLine}{Exception}").SetMinimumLevel(LogLevel.Debug);
            });

            this._logger = this._loggerFactory.CreateLogger<App>();
            this._logger.LogInformation($"{nameof(App)} - Start");
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!File.Exists(this._configurationFile))
            {
                this._logger.LogError($"{nameof(Application_Startup)} - Configuration file not available, {this._configurationFile}");
                Current.Shutdown(-2);
                return;
            }

            Parser.Default.ParseArguments<CommandLineOptions>(e.Args)
                .WithParsed(this.RunOptions)
                .WithNotParsed(this.HandleParseError);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            this._loggerFactory.Dispose();
            this._logger.LogInformation($"{nameof(Application_Exit)} - Exit");
        }

        private PaymentTerminalConfig GetConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(this._configurationFile, optional: false);

            IConfigurationRoot configuration = builder.Build();

            if (!int.TryParse(configuration["Port"], out var port))
            {
                this._logger.LogError($"{nameof(GetConfiguration)} - Cannot parse port from configuration file");
            }

            return new PaymentTerminalConfig
            {
                IpAddress = configuration["IpAddress"],
                Port = port
            };
        }

        private void RunOptions(CommandLineOptions options)
        {
            this._logger.LogInformation($"{nameof(RunOptions)} - Startup successful, start payment process wiht an amount of {options.Amount}");

            var configuration = this.GetConfiguration();

            var window = new MainWindow(this._loggerFactory, configuration, options.Amount);
            window.Show();
        }

        private void HandleParseError(IEnumerable<Error> errors)
        {
            Current.Shutdown(-1);
        }
    }
}
