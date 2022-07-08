# Portalum.Zvt - ZVT Client for .NET (C#)

<img src="https://raw.githubusercontent.com/Portalum/Portalum.Zvt/main/doc/logo.png" width="150" title="Portalum Zvt Client" alt="Portalum Zvt Client" align="left">

Portalum.Zvt is a library designed to simplify communication with payment terminals via the **ZVT Protocol**. The library is based on Microsoft .NET.

Communication via Network (TCP) and communication via a serial connection is supported. The most important commands for processing a payment transaction with an electronic POS system are also already integrated.

The aim of this project is to achieve uncomplicated acceptance by payment service providers. The more often this project is referred to, the better it should work. Please help us to achieve this.
<br>
<br>

## How can I use it?

The package is available via [NuGet](https://www.nuget.org/packages/Portalum.Zvt)
```
PM> install-package Portalum.Zvt
```

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

## Important information for the start

Before sending a payment to the terminal, you should consider how to configure the terminal. For example, it can be set that a manual start of a payment at the terminal is no longer possible. You must also set where the receipts are printed directly via the terminal or via an external printer. For the configuration use the `Registration` command.

## Examples

Here you can find some code examples how to use this library

### Start Payment
```cs
var deviceCommunication = new TcpNetworkDeviceCommunication("192.168.0.10");
if (!await deviceCommunication.ConnectAsync())
{
    return;
}

using var zvtClient = new ZvtClient(deviceCommunication);
zvtClient.StatusInformationReceived += (statusInformation) => Console.WriteLine(statusInformation.ErrorMessage);
await zvtClient.PaymentAsync(10.5M);
```

### Start End-of-day
```cs
var deviceCommunication = new TcpNetworkDeviceCommunication("192.168.0.10");
if (!await deviceCommunication.ConnectAsync())
{
    return;
}

using var zvtClient = new ZvtClient(deviceCommunication);
zvtClient.StatusInformationReceived += (statusInformation) => Console.WriteLine(statusInformation.ErrorMessage);
await zvtClient.EndOfDayAsync();
```

### Set a custom configuration
```cs
var deviceCommunication = new TcpNetworkDeviceCommunication("192.168.0.10");
if (!await deviceCommunication.ConnectAsync())
{
    return;
}

var clientConfig = new ZvtClientConfig
{
    Encoding = ZvtEncoding.CodePage437,
    Language = Language.German,
    Password = 000000
};
var zvtClient = new ZvtClient(deviceCommunication, clientConfig: clientConfig);
```

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
if (!await deviceCommunication.ConnectAsync())
{
    return;
}

var zvtClient = new ZvtClient(deviceCommunication, logger: zvtClientLogger);
```

### Set a custom tcp port

```
var deviceCommunication = new TcpNetworkDeviceCommunication("192.168.0.10", port: 20007);
```

## ControlPanel
With the Portalum.Zvt.ControlPanel you can test the different ZVT functions.

**To use the tool, the following steps must be performed**

- Install at least [.NET Desktop Runtime 6.0.3](https://dotnet.microsoft.com/download/dotnet/6.0)
- Download and extract the ControlPanel ([download](https://github.com/Portalum/Portalum.Zvt/releases/latest/download/Portalum.Zvt.ControlPanel.zip))

![Portalum.Zvt.ControlPanel](/doc/ControlPanel.png)

## EasyPay
When you only want to transmit an amount to the payment terminal.<br>
Then you can still look at our small tool. [Portalum.Zvt.EasyPay](https://github.com/Portalum/Portalum.Zvt.EasyPay)

## Tested Providers and Terminals

We have already been able to test the terminals of these payment service providers.

Provider | Country | Terminal | 
--- | --- | --- |
CardComplete | Austria | ingenico iWL250 |
Hobex | Austria | ingenico Desk/3500 |
Wordline/PAYONE (SIX) | Austria | yomani touch family |
Global Payments | Austria | PAX A80 |

### Known deviations from the standard ZVT protocol

#### CardComplete
- Encoding is fixed to `ISO-8859-1/ISO-8859-2/ISO-8859-15` instead of default character set `CP437`. There is no way to configure this
- `Print Line` contains TLV data at the end of the package, after `TLV-activation`. According to official documentation, there should be no TLV data here

#### Hobex
- No `Print Line` support
- Sends TLV data even without `TLV-activation`

#### Global Payments
- Abort from ECR not possible 

## General info on the connection of payment terminals

### TCP

Common `Ports` of the device are 20007, 20008

### Serial

Common `BaudRates` is *9600* or *115200*, default `Parity` is *None*, default `DataBits` is *8*, default `StopBits` is *2*

## ZVT Documentation

The official documentation of the ZVT protocol is available here

- https://www.terminalhersteller.de
- https://www.terminalhersteller.de/downloads/PA00P015_13.09_final_en.pdf
- https://www.terminalhersteller.de/downloads/PA00P016_04_en.pdf
