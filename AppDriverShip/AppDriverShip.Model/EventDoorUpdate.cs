namespace AppDriverShip.Model;

internal class EventDoorUpdate
{
	public string DoorName { get; set; }

	public string ExtDoorID { get; set; }

	public int BatteryStatus { get; set; }

	public bool NeedUpdate { get; set; }
}
