using System.Collections.Generic;
using System.Xml.Serialization;

namespace Apis.Salto.Ship.Model;

public class DesfireBinaryData
{
	[XmlArray("FileBinaryDataList")]
	[XmlArrayItem("FileBinaryData")]
	public List<DesfireFileBinaryData> Files { get; set; } = new List<DesfireFileBinaryData>();

}
