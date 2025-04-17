using System.Collections.Generic;
using System.Xml.Serialization;

namespace Apis.Salto.Ship.Model;

public class MifareBinaryData
{
	[XmlArray("BlockBinaryDataList")]
	[XmlArrayItem("BlockBinaryData")]
	public List<BlockBinaryData> BlockBinaryData { get; set; } = new List<BlockBinaryData>();

}
