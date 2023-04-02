using System.Xml.Serialization;

namespace L2EliminateUpdater.Models
{
    [Serializable]
    [XmlRoot(ElementName = "list")]
    public class FileList
    {
        public FileList() { }
        [XmlElement(ElementName = "set")]
        public List<Set> Set { get; set; }
    }
}
