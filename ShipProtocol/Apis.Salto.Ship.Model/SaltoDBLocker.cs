namespace Apis.Salto.Ship.Model;

public class SaltoDBLocker
{
	public string ExtDoorID { get; set; }

	public string Name { get; set; }

	public string Description { get; set; }

	public string GPF1 { get; set; }

	public int OpenTime { get; set; } = 4;


	public int OpenTimeADA { get; set; } = 10;


	public int AuditOnKeys { get; set; }

	public int UpdateRequired { get; set; }

	public int BatteryStatus { get; set; } = -1;


	public int IsFreeAssignment { get; set; }

	public override string ToString()
	{
		return $"Locker: ExtDoorId={ExtDoorID} Name={Name} Description={Description} BatteryStatus={BatteryStatus} ";
	}

	public static explicit operator SaltoDBDoor(SaltoDBLocker l)
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
