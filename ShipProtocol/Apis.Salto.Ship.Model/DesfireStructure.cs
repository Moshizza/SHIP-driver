using System.Collections.Generic;
using System.Xml.Serialization;

namespace Apis.Salto.Ship.Model;

public class DesfireStructure
{
	public string DesfireCardSerialNumber { get; set; }

	[XmlArray("FileList")]
	[XmlArrayItem("File")]
	public List<DesfireFile> Files { get; set; } = new List<DesfireFile>();

}
