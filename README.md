<img src="https://raw.githubusercontent.com/Portalum/Portalum.Payment.Zvt/main/doc/logo.png" width="200">

# Portalum.Payment.Zvt

Portalum.Payment.Zvt is a library designed to simplify communication with payment terminals via the ZVT Protocol. The library is based on Microsoft .NET. Communication via TCP/IP is supported and communication via a serial connection is also provided. The most important commands for processing a payment transaction with an electronic POS system are also already integrated.

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

The package is available via [nuget](https://www.nuget.org/packages/Portalum.Payment.Zvt)
```
PM> install-package Portalum.Payment.Zvt
```

## Examples

### Start payment prcocess
```cs
var deviceCommunication = new TcpNetworkDeviceCommunication("1.2.3.4", port: 20007);
await deviceCommunication.ConnectAsync();

using var zvtClient = new ZvtClient(deviceCommunication);
await zvtClient.PaymentAsync(10.5M);
```

### End-of-day
```cs
var deviceCommunication = new TcpNetworkDeviceCommunication("1.2.3.4", port: 20007);
await deviceCommunication.ConnectAsync();

using var zvtClient = new ZvtClient(deviceCommunication);
await zvtClient.EndOfDayAsync();
```

## TestUi
With the Portalum.Payment.Zvt.TestUi you can test the different ZVT functions.

### To use the tool, the following steps must be performed

- Install [.NET Desktop Runtime 5.x](https://dotnet.microsoft.com/download/dotnet/5.0)
- Download and extract the TestUi ([download](https://github.com/Portalum/Portalum.Payment.Zvt/releases/latest/download/Portalum.Payment.Zvt.TestUi.zip))

![Portalum.Payment.TestUi](/doc/TestUi.png)

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
