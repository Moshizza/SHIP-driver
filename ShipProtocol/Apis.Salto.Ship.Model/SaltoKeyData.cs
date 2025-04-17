using System.Xml.Serialization;

namespace Apis.Salto.Ship.Model;

public class SaltoKeyData
{
	public int Office { get; set; }

	public int Privacy { get; set; }

	public int AuditOpenings { get; set; } = 1;


	public int AuditRejections { get; set; } = 1;


	public int LastReject { get; set; }

	public int LowBatteryWarning { get; set; }

	public int UseAntipassback { get; set; }

	public Period GeneralPeriod { get; set; }

	[XmlArray("AccessRightList")]
	[XmlArrayItem("AccessRight")]
	public AccessRight[] AccessRightList { get; set; }

	public int PINEnabled { get; set; }

	public string PINCode { get; set; }

	[XmlArray("OutputList")]
	[XmlArrayItem("OutputID")]
	public string[] OutputList { get; set; }
}
