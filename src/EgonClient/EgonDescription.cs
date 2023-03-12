namespace EgonAPI
{
    public class EgonDescription
    {
        /// <summary>
        /// MAC address.
        /// </summary>
        public string MAC { get; private set; }

        /// <summary>
        /// Egon port.
        /// </summary>
        public string Port { get; private set; }

        /// <summary>
        /// IP Address.
        /// </summary>
        public string IpAddr { get; private set; }

        /// <summary>
        /// IP Mask.
        /// </summary>
        public string Mask { get; private set; }

        /// <summary>
        /// IP Gateway.
        /// </summary>
        public string Gateway { get; private set; }
        
        /// <summary>
        /// DNS IP address.
        /// </summary>
        public string Dns1 { get; private set; }

        /// <summary>
        /// Egon version.
        /// </summary>
        public string Version { get; private set; }
    }
}
