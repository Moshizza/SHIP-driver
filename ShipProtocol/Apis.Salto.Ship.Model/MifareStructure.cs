using System.Collections.Generic;
using System.Xml.Serialization;

namespace Apis.Salto.Ship.Model;

public class MifareStructure
{
	public string MifareCardSerialNumber { get; set; }

	[XmlArray("SectorIDList")]
	[XmlArrayItem("SectorID")]
	public List<int> SectorsIds { get; set; } = new List<int>();

}
