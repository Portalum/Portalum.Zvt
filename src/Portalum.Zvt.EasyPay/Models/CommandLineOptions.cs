using CommandLine;

namespace Portalum.Zvt.EasyPay.Models
{
    public class CommandLineOptions
    {
        [Option(Required = true, HelpText = "The amount to pay. For example 20.2")]
        [Value(0)]
        public decimal Amount { get; set; }
    }
}
