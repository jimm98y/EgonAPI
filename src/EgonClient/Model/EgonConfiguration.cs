using System.Collections.Generic;

namespace EgonAPI.Model
{
    public class EgonConfiguration
    {
        public IDictionary<string, EgonDevice> Devices { get; set; }

        public IList<EgonGroup> Groups { get; set; }
    }
}
