using System;
using System.Collections.Generic;
using System.Linq;

namespace EgonAPI
{
    public class EgonDescription
    {
        public string MAC { get; private set; }
        public string Port { get; private set; }
        public string IpAddr { get; private set; }
        public string Mask { get; private set; }
        public string Gateway { get; private set; }
        public string Dns1 { get; private set; }
        public string Version { get; private set; }

        private EgonDescription(string mac, string port, string ipaddr, string mask, string gateway, string dns1, string version)
        {
            this.MAC = mac;
            this.Port = port;
            this.IpAddr = ipaddr;
            this.Mask = mask;
            this.Gateway = gateway;
            this.Dns1 = dns1;
            this.Version = version;
        }

        /// <summary>
        /// Create description from the broadcast respose.
        /// </summary>
        /// <param name="response">Response example: EGO-N,MAC=XX:XX:XX:XX:XX:XX,PORT=2af9,IPADDR=192.168.1.XXX,MASK=255.255.255.0,GATEWAY=192.168.1.1,DNS1=192.168.1.1,VERSION=25</param>
        /// <returns><see cref="EgonDescription"/>.</returns>
        /// <exception cref="NotSupportedException"></exception>
        public static EgonDescription FromBroadcastResponse(string response)
        {
            if (response.StartsWith("EGO-N,"))
                response = response.Substring(6);
            else
                throw new NotSupportedException(response);

            string[] splittedProperties = response.Split(',', '=');
            if (splittedProperties.Count() < 14)
                throw new NotSupportedException();

            var result = new Dictionary<string, string>();
            for (var i = 0; i < splittedProperties.Length; i += 2)
            {
                result.Add(splittedProperties[i], splittedProperties[i + 1]);
            }

            return new EgonDescription(
                result["MAC"],
                result["PORT"],
                result["IPADDR"],
                result["MASK"],
                result["GATEWAY"],
                result["DNS1"],
                result["VERSION"]);
        }
    }
}
