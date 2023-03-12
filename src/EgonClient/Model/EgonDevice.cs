using EgonAPI.API;

namespace EgonAPI.Model
{
    public class EgonDevice
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string DeviceType { get; set; }

        public bool IsEnabled { get; set; }

        public string CurrentValue { get; set; }
    }
}
