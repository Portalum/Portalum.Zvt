<img src="https://raw.githubusercontent.com/Portalum/Portalum.Payment.Zvt/main/doc/logo.png" width="200">

# Portalum.Payment.Zvt
ZVT standard cash register interface

This library is intended to simplify the first steps with the ZVT protocol so that you can immediately communicate with your payment terminal.
Our implementation is designed for communication via network. But it should also be possible to communicate via Serial without much effort.
It is also possible to easily integrate the whole thing into a web service using this implementation.

## Supported features

- Registration
- Log-Off
- Authorization
- Reversal
- End-of-Day
- Send Turnover Totals
- Repeat Receipt
- Diagnosis

## How can I use it?
The package is available via [nuget](https://www.nuget.org/packages/Portalum.Payment.Zvt)
```
PM> install-package Portalum.Payment.Zvt
```

## ZVT Documentation
- https://www.terminalhersteller.de/downloads/PA00P015_13.09_final_en.pdf
- https://www.terminalhersteller.de/downloads/PA00P016_04_en.pdf
