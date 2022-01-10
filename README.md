# Portalum.Zvt - ZVT Client for .NET

<img src="https://raw.githubusercontent.com/Portalum/Portalum.Zvt/main/doc/logo.png" width="100" title="Portalum Zvt Client" alt="Portalum Zvt Client" align="left">

Portalum.Zvt is a library designed to simplify communication with payment terminals via the ZVT Protocol. The library is based on Microsoft .NET. Communication via TCP/IP is supported and communication via a serial connection is also provided. The most important commands for processing a payment transaction with an electronic POS system are also already integrated.

<br>
<br>

## Supported features

### Commands to Payment Terminal

- Registration
- Log-Off
- Authorization
- Reversal
- Refund
- End-of-Day
- Send Turnover Totals
- Repeat Receipt
- Diagnosis

### Commands from Payment Terminal

- Status-Information
- Intermediate StatusInformation
- Print Line
- Print Text-Block

### Generic

- BMP Processing
- TLV Processing

## How can I use it?

The package is available via [nuget](https://www.nuget.org/packages/Portalum.Zvt)
```
PM> install-package Portalum.Zvt
```

## Important information for the start

Before sending a payment to the terminal, you should consider how to configure the terminal. For example, it can be set that a manual start of a payment at the terminal is no longer possible. You must also set where the receipts are printed directly via the terminal or via an external printer. For the configuration use the `Registration` command.

## Examples

### Activate logging

This library uses the `Microsoft.Extensions.Logging` package so you can easily decide where to write the log files, to a file or directly to the console output for example.
To write the logging output directly to the console output, this nuget packages is needed `Microsoft.Extensions.Logging.Console`.

```cs
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole().SetMinimumLevel(LogLevel.Debug);
});

var deviceCommunicationLogger = loggerFactory.CreateLogger<TcpNetworkDeviceCommunication>();
var zvtClientLogger = loggerFactory.CreateLogger<ZvtClient>();

var deviceCommunication = new TcpNetworkDeviceCommunication("192.168.0.10", logger: deviceCommunicationLogger);
var zvtClient = new ZvtClient(deviceCommunication, logger: zvtClientLogger);
```

### Set a custom terminal password

```
var deviceCommunication = new TcpNetworkDeviceCommunication("192.168.0.10");
var zvtClient = new ZvtClient(deviceCommunication, password: 123456);
```

### Set a custom network device port

```
var deviceCommunication = new TcpNetworkDeviceCommunication("192.168.0.10", port: 20007);
```

### Start payment prcocess
```cs
var deviceCommunication = new TcpNetworkDeviceCommunication("192.168.0.10");
await deviceCommunication.ConnectAsync();

using var zvtClient = new ZvtClient(deviceCommunication);
zvtClient.StatusInformationReceived += (statusInformation) => Console.WriteLine(statusInformation.ErrorMessage);
await zvtClient.PaymentAsync(10.5M);
```

### End-of-day
```cs
var deviceCommunication = new TcpNetworkDeviceCommunication("192.168.0.10");
await deviceCommunication.ConnectAsync();

using var zvtClient = new ZvtClient(deviceCommunication);
zvtClient.StatusInformationReceived += (statusInformation) => Console.WriteLine(statusInformation.ErrorMessage);
await zvtClient.EndOfDayAsync();
```

## TestUi
With the Portalum.Zvt.TestUi you can test the different ZVT functions.

### To use the tool, the following steps must be performed

- Install [.NET Desktop Runtime 5.x](https://dotnet.microsoft.com/download/dotnet/5.0)
- Download and extract the TestUi ([download](https://github.com/Portalum/Portalum.Zvt/releases/latest/download/Portalum.Zvt.TestUi.zip))

![Portalum.Zvt.TestUi](/doc/TestUi.png)

## Tested Providers and Terminals

Provider | Terminal | 
--- | --- |
CardComplete | ingenico iWL250 |
Hobex | ingenico Desk/3500 |
Wordline (SIX) | yomani touch family |

### Known deviations from the standard ZVT protocol

#### CardComplete
- Encoding is fixed to `UTF-8` instead of default character set `CP437`. There is no way to configure this
- `Print Line` contains TLV data at the end of the package, after `TLV-activation`. According to official documentation, there should be no TLV data here

#### Hobex
- No `Print Line` support
- Sends TLV data even without `TLV-activation`

## ZVT Documentation
- https://www.terminalhersteller.de/downloads/PA00P015_13.09_final_en.pdf
- https://www.terminalhersteller.de/downloads/PA00P016_04_en.pdf
