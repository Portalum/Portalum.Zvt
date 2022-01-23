# Portalum.Zvt - ZVT Client for .NET

<img src="https://raw.githubusercontent.com/Portalum/Portalum.Zvt/main/doc/logo.png" width="100" title="Portalum Zvt Client" alt="Portalum Zvt Client" align="left">

Portalum.Zvt is a library designed to simplify communication with payment terminals via the ZVT Protocol. The library is based on Microsoft .NET. Communication via TCP/IP is supported and communication via a serial connection is also provided. The most important commands for processing a payment transaction with an electronic POS system are also already integrated.

<br>
<br>

## Supported features

The following features of the ZVT protocol were implemented.

### Commands to Payment Terminal

Commands sent from the cash register to the payment terminal

- Registration
- Log-Off
- Authorization
- Reversal
- Refund
- End-of-Day
- Send Turnover Totals
- Repeat Receipt
- Diagnosis
- Abort
- Software-Update

### Commands from Payment Terminal

Information sent from the payment terminal to the cash register

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

Here you can find some code examples how to use this library

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

## ControlPanel
With the Portalum.Zvt.ControlPanel you can test the different ZVT functions.

**To use the tool, the following steps must be performed**

- Install [.NET Desktop Runtime 6.x](https://dotnet.microsoft.com/download/dotnet/6.0)
- Download and extract the ControlPanel ([download](https://github.com/Portalum/Portalum.Zvt/releases/latest/download/Portalum.Zvt.ControlPanel.zip))

![Portalum.Zvt.ControlPanel](/doc/ControlPanel.png)

## Tested Providers and Terminals

We have already been able to test the terminals of these payment service providers.

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

The official documentation of the ZVT protocol is available here

- https://www.terminalhersteller.de/downloads/PA00P015_13.09_final_en.pdf
- https://www.terminalhersteller.de/downloads/PA00P016_04_en.pdf
