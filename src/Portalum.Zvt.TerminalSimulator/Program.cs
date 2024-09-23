using Portalum.Zvt.Helpers;
using SuperSimpleTcp;
using System.Runtime.CompilerServices;

class Program
{
    private static readonly byte[] _commandCompletionPackage = new byte[] { 0x80, 0x00, 0x00 };
    private static readonly byte[] _completionPackage = new byte[] { 0x06, 0x0F, 0x00 }; //3.2 Completion

    private static SimpleTcpServer? _tcpServer;

    static void Main(string[] args)
    {
        _tcpServer = new SimpleTcpServer("127.0.0.1", 20007);
        _tcpServer.Events.ClientConnected += Events_ClientConnected;
        _tcpServer.Events.ClientDisconnected += Events_ClientDisconnected;
        _tcpServer.Events.DataReceived += Events_DataReceived;
        _tcpServer.Start();

        Console.WriteLine("Virtual Terminal ready on 127.0.0.1:20007");
        Console.WriteLine("Wait for connections, press any key for quit");
        Console.ReadLine();

        _tcpServer.Events.ClientConnected -= Events_ClientConnected;
        _tcpServer.Events.ClientDisconnected -= Events_ClientDisconnected;
        _tcpServer.Events.DataReceived -= Events_DataReceived;
        _tcpServer.Stop();
        _tcpServer.Dispose();
    }

    private static void Events_ClientConnected(object? sender, ConnectionEventArgs e)
    {
        Console.WriteLine($"ClientConnected - {e.IpPort}");
    }

    private static void Events_ClientDisconnected(object? sender, ConnectionEventArgs e)
    {
        Console.WriteLine($"ClientDisconnected - {e.IpPort}");
    }

    private static bool IsClientConnected(string ipPort)
    {
        if (_tcpServer == null)
        {
            return false;
        }

        var clients = _tcpServer.GetClients();
        return clients.Contains(ipPort);
    }

    private static void Events_DataReceived(object? sender, DataReceivedEventArgs e)
    {
        if (_tcpServer == null)
        {
            return;
        }

        var hexData = BitConverter.ToString(e.Data.ToArray());

        var data = e.Data.AsSpan();

        // Positive acknowledgement
        if (data.StartsWith(new byte[] { 0x80, 0x00, 0x00 }))
        {
            Console.WriteLine($"Receive Positive acknowledgement - [{hexData}]");
            return;
        }

        // Registration (06 00)
        if (data.StartsWith(new byte[] { 0x06, 0x00 }))
        {
            Console.WriteLine($"Receive Registration - [{hexData}]");

            Thread.Sleep(500);

            Console.WriteLine("Send Command Completion");
            _tcpServer.Send(e.IpPort, _commandCompletionPackage);

            Thread.Sleep(1000);

            Console.WriteLine("Send Completion");
            _tcpServer.Send(e.IpPort, _completionPackage);

            return;
        }

        //Authorization (06 01)
        if (data.StartsWith(new byte[] { 0x06, 0x01 }))
        {
            //Step 1

            var amountBytes = data.Slice(4);
            var amount = NumberHelper.BcdToDecimal(amountBytes.ToArray());

            Console.WriteLine($"Receive Authorization with an amount {amount}EUR - [{hexData}]");

            Thread.Sleep(500);

            if (!IsClientConnected(e.IpPort))
            {
                Console.WriteLine("Failure - Client is not connected");
                return;
            }

            Console.WriteLine("Send Command Completion");
            _tcpServer.Send(e.IpPort, _commandCompletionPackage);

            Thread.Sleep(1000);

            // Step 2

            if (!IsClientConnected(e.IpPort))
            {
                Console.WriteLine("Failure - Client is not connected");
                return;
            }

            Console.WriteLine("Send Message - Insert Card");
            var waitForCardMessage = new byte[] { 0x04, 0xFF, 0x01, 0x0A };
            _tcpServer.Send(e.IpPort, waitForCardMessage);

            Thread.Sleep(2000);

            // Step 3

            if (!IsClientConnected(e.IpPort))
            {
                Console.WriteLine("Failure - Client is not connected");
                return;
            }

            Console.WriteLine("Send Status-Information");
            var byteData = new byte[] { 0x04, 0x0F, 0x77, 0x27, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x20, 0x40, 0x0B, 0x00, 0x04, 0x70, 0x0C, 0x22, 0x39, 0x53, 0x0D, 0x10, 0x06, 0x0E, 0x25, 0x12, 0x17, 0x00, 0x01, 0x19, 0x70, 0x22, 0xF0, 0xF8, 0xEE, 0xEE, 0xEE, 0xEE, 0xEE, 0xEE, 0x47, 0x71, 0x29, 0x28, 0x00, 0x48, 0x69, 0x2A, 0x31, 0x30, 0x30, 0x34, 0x36, 0x31, 0x37, 0x36, 0x33, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x3B, 0x36, 0x31, 0x31, 0x38, 0x30, 0x34, 0x00, 0x00, 0x3C, 0xF0, 0xF1, 0xF4, 0x47, 0x45, 0x4E, 0x2E, 0x4E, 0x52, 0x2E, 0x3A, 0x36, 0x31, 0x31, 0x38, 0x30, 0x34, 0x49, 0x09, 0x78, 0x87, 0x04, 0x70, 0x88, 0x00, 0x04, 0x70, 0x8A, 0x2E, 0x8B, 0xF1, 0xF7, 0x44, 0x65, 0x62, 0x69, 0x74, 0x20, 0x4D, 0x61, 0x73, 0x74, 0x65, 0x72, 0x63, 0x61, 0x72, 0x64, 0x00 };
            _tcpServer.Send(e.IpPort, byteData);

            Thread.Sleep(1000);

            // Step 4

            if (!IsClientConnected(e.IpPort))
            {
                Console.WriteLine("Failure - Client is not connected");
                return;
            }

            Console.WriteLine("Send Completion");
            _tcpServer.Send(e.IpPort, _completionPackage);

            return;
        }

        // Log-Off (06 02)
        if (data.StartsWith(new byte[] { 0x06, 0x02 }))
        {
            Console.WriteLine($"Receive Log-Off - [{hexData}]");

            Thread.Sleep(500);

            Console.WriteLine("Send Command Completion");
            _tcpServer.Send(e.IpPort, _commandCompletionPackage);

            return;
        }

        Console.WriteLine($"Unknown command for simulator - [{hexData}]");
    }
}