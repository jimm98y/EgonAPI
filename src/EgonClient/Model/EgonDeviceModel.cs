using EgonAPI.API;

namespace EgonAPI.Model
{
    public class EgonDeviceModel
    {
        public string Type { get { return nameof(EgonDeviceModel); } }

        public string Id { get; set; }

        public string Name { get; set; }

        public string DeviceType { get; set; }

        public bool IsEnabled { get; set; }

        public string CurrentValue { get; set; }

        internal static EgonDeviceModel FromData(EgonElement e)
        {
            EgonDeviceModel d = new EgonDeviceModel();
            d.CurrentValue = e.Value;
            d.Id = e.Id;
            d.IsEnabled = "true".CompareTo(e.Enabled) == 0;
            d.Name = e.Name;
            d.DeviceType = e.Type;
            return d;
        }
    }
}
