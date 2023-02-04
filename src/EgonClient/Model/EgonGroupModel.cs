using EgonAPI.API;
using System.Collections.Generic;
using System.Linq;

namespace EgonAPI.Model
{
    public class EgonGroupModel
    {
        public string Type { get { return nameof(EgonGroup); } }

        public string Id { get; set; }

        public string Name { get; set; }

        public IList<string> Devices { get; set; }

        internal static EgonGroupModel FromData(EgonGroup g, EgonElementState[] element_states)
        {
            EgonGroupModel eg = new EgonGroupModel();
            eg.Id = g.Id;
            eg.Name = g.Name;
            eg.Devices = element_states.Select(x => x.Id).ToList();
            return eg;
        }
    }
}
