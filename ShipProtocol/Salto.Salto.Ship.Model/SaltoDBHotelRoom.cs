using System.Collections.Generic;
using System.Xml.Serialization;
using Apis.Salto.Ship.Model;

namespace Salto.Salto.Ship.Model;

public class SaltoDBHotelRoom
{
	public string ExtDoorID { get; set; }

	public string Name { get; set; }

	public string Description { get; set; }

	public string GPF1 { get; set; }

	public string GPF2 { get; set; }

	public int OpenTime { get; set; }

	public int OpenTimeADA { get; set; }

	public int AuditOnKeys { get; set; }

	public int UpdateRequired { get; set; }

	public int BatteryStatus { get; set; }

	public string ExtParentHotelSuiteID { get; set; }

	[XmlArray("SaltoDB.MembershipList.Door_Zone")]
	[XmlArrayItem("SaltoDB.Membership.Door_Zone")]
	private List<Door_Zone> Door_Zones { get; set; }

	public override string ToString()
	{
		return $"Locker: ExtDoorId={ExtDoorID} Name={Name} Description={Description} BatteryStatus={BatteryStatus} ";
	}

	public static explicit operator SaltoDBDoor(SaltoDBHotelRoom l)
	{
		return new SaltoDBDoor
		{
			ExtDoorID = l.ExtDoorID,
			Name = l.Name,
			Description = l.Description,
			GPF1 = l.GPF1,
			OpenTime = l.OpenTime,
			OpenTimeADA = l.OpenTimeADA,
			AuditOnKeys = l.AuditOnKeys,
			UpdateRequired = l.UpdateRequired,
			BatteryStatus = l.BatteryStatus
		};
	}
}
