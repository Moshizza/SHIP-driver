using System.Xml.Serialization;

namespace Apis.Salto.Ship.Model;

public class AccessRight
{
	public AccessPoint AccessPoint { get; set; }

	[XmlArray("TimezoneList")]
	[XmlArrayItem("Timezone")]
	public Timezone[] TimezoneList { get; set; }

	public Period Period { get; set; }
}
