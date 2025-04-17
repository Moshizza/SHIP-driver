using System.Collections.Generic;
using System.Xml.Serialization;

namespace Apis.Salto.Ship.Model;

public class SaltoDBZone
{
	public string ExtZoneID { get; set; }

	public string Name { get; set; }

	public string Description { get; set; }

	public int IsLow { get; set; }

	public int IsFreeAssignment { get; set; }

	public int FreeAssignmentGroup { get; set; } = 1;


	[XmlArray("SaltoDB.MembershipList.Door_Zone")]
	[XmlArrayItem("SaltoDB.Membership.Door_Zone")]
	public List<Door_Zone> Door_Zones { get; set; } = new List<Door_Zone>();


	public override string ToString()
	{
		return $"SaltoDBZone: ExtZoneID={ExtZoneID} Name={Name} Description={Description} IsLow={IsLow} IsFreeAssignment={IsFreeAssignment}  FreeAssignmentGroup={FreeAssignmentGroup} ";
	}
}
