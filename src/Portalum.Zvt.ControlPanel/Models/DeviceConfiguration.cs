namespace Portalum.Zvt.ControlPanel.Models
{
    public class DeviceConfiguration
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public ZvtEncoding Encoding { get; set; }
        public Language Language { get; set; }
        public bool TcpKeepalive { get; set; }
    }
}
