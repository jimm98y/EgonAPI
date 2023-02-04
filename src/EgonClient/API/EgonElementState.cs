using System.Xml.Serialization;

namespace EgonAPI.API
{
    [XmlRoot(ElementName = "element_state", Namespace = "")]
    public class EgonElementState
    {
        [XmlAttribute(AttributeName = "id", Namespace = "")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "value", Namespace = "")]
        public string Value { get; set; }
    }
}
