using System.Xml.Serialization;

namespace EgonAPI.API
{
    [XmlRoot(ElementName = "egon_data", Namespace = "")]
    public class EgonData
    {
        [XmlArray(ElementName = "elements", Namespace = "")]
        [XmlArrayItem("element", Type = typeof(EgonElement))]
        public EgonElement[] Elements { get; set; }

        [XmlArray(ElementName = "groups", Namespace = "")]
        [XmlArrayItem("group", Type = typeof(EgonGroup))]
        public EgonGroup[] Groups { get; set; }

        [XmlArray(ElementName = "element_states", Namespace = "")]
        [XmlArrayItem("element_state", Type = typeof(EgonElementState))]
        public EgonElementState[] ElementStates { get; set; }
    }
}
