using System.Collections.Generic;
using System.Xml.Serialization;

namespace Apis.Salto.Ship.Model;

public class SaltoDBDoor
{
	public string ExtDoorID { get; set; }

	public string Name { get; set; }

	public string Description { get; set; }

	public string GPF1 { get; set; }

	public string GPF2 { get; set; }

	public int OpenTime { get; set; } = 4;


	public int OpenTimeADA { get; set; } = 10;


	public int OpeningMode { get; set; }

	public int TimedPeriodsTableID { get; set; } = 1;


	public int AutomaticChangesTableID { get; set; } = 1;


	public string KeypadCode { get; set; }

	public int AuditOnKeys { get; set; }

	public int AntipassbackEnabled { get; set; }

	public int OutwardAntipassback { get; set; }

	public int UpdateRequired { get; set; }

	public int BatteryStatus { get; set; } = -1;


	[XmlArray("SaltoDB.MembershipList.Door_Zone")]
	[XmlArrayItem("SaltoDB.Membership.Door_Zone")]
	private List<Door_Zone> Door_Zones { get; set; }

	[XmlArray("SaltoDB.MembershipList.Door_Location")]
	[XmlArrayItem("SaltoDB.Membership.Door_Location")]
	private List<Door_Location> Door_Locations { get; set; }

	[XmlArray("SaltoDB.MembershipList.Door_Function")]
	[XmlArrayItem("SaltoDB.Membership.Door_Function")]
	private List<Door_Function> Door_Functions { get; set; }

	public override string ToString()
	{
		return $"Door: ExtDoorId={ExtDoorID} Name={Name} Description={Description} BatteryStatus={BatteryStatus} ";
	}
}
