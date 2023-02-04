using System.Xml.Serialization;

namespace EgonAPI.API
{
    [XmlRoot(ElementName = "element", Namespace = "")]
    public class EgonElement
    {
        [XmlAttribute(AttributeName = "id", Namespace = "")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "name", Namespace = "")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "type", Namespace = "")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value", Namespace = "")]
        public string Value { get; set; }

        [XmlAttribute(AttributeName = "enabled", Namespace = "")]
        public string Enabled { get; set; }
    }
}
