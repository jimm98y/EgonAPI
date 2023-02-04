using System.Collections.Generic;

namespace EgonAPI.Model
{
    public class EgonConfigurationModel
    {
        public string Type { get { return nameof(EgonConfigurationModel); } }

        public IList<EgonDeviceModel> Devices { get; set; }

        public IList<EgonGroupModel> Groups { get; set; }
    }
}
