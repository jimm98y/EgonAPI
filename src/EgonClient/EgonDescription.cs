namespace EgonAPI
{
    public class EgonDescription
    {
        /// <summary>
        /// MAC address.
        /// </summary>
        public string MAC { get; set; }

        /// <summary>
        /// Egon port.
        /// </summary>
        public string Port { get; set; }

        /// <summary>
        /// IP Address.
        /// </summary>
        public string IpAddr { get; set; }

        /// <summary>
        /// IP Mask.
        /// </summary>
        public string Mask { get; set; }

        /// <summary>
        /// IP Gateway.
        /// </summary>
        public string Gateway { get; set; }
        
        /// <summary>
        /// DNS IP address.
        /// </summary>
        public string Dns1 { get; set; }

        /// <summary>
        /// Egon version.
        /// </summary>
        public string Version { get; set; }
    }
}
