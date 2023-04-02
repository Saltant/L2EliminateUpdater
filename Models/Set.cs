using System.Xml.Serialization;

namespace L2EliminateUpdater.Models
{
    [XmlRoot(ElementName = "set")]
    public class Set
    {
        [XmlAttribute(AttributeName = "file")]
        public string File { get; set; }
        [XmlAttribute(AttributeName = "hash")]
        public string Hash { get; set; }
        [XmlAttribute(AttributeName = "size")]
        public string Size { get; set; }
    }
}
