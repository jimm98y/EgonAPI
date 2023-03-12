using System.Collections.Generic;

namespace EgonAPI.Model
{
    public class EgonGroup
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public IList<string> Devices { get; set; }
    }
}
