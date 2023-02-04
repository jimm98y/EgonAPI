using System.Xml.Serialization;

namespace EgonAPI.API
{
    [XmlRoot(ElementName = "group", Namespace = "")]
    public class EgonGroup
    {
        [XmlAttribute(AttributeName = "id", Namespace = "")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "name", Namespace = "")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "enabled", Namespace = "")]
        public string Enabled { get; set; }

        [XmlAttribute(AttributeName = "default", Namespace = "")]
        public string IsDefault { get; set; }

        [XmlArray(ElementName = "elements", Namespace = "")]
        [XmlArrayItem("element", Type = typeof(EgonElement))]
        public EgonElement[] Elements { get; set; }
    }
}
