<img src="https://raw.githubusercontent.com/Portalum/Portalum.Payment.Zvt/main/doc/logo.png" width="200">

# Portalum.Payment.Zvt
ZVT standard cash register interface

This library is intended to simplify the first steps with the ZVT protocol so that you can immediately communicate with your payment terminal.
Our implementation is designed for communication via network. But it should also be possible to communicate via Serial without much effort.
It is also possible to easily integrate the whole thing into a web service using this implementation.

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

## ZVT Documentation
- https://www.terminalhersteller.de/downloads/PA00P015_13.09_final_en.pdf
- https://www.terminalhersteller.de/downloads/PA00P016_04_en.pdf
