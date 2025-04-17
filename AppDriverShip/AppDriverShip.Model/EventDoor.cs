using System;

namespace AppDriverShip.Model;

internal class EventDoor
{
	public int EventID { get; set; }

	public string DoorID { get; set; }

	public string SubjectID { get; set; }

	public DateTimeOffset DateUtc { get; set; }

	public override string ToString()
	{
		return $"SaltoEvent: EventID={EventID} DoorID={DoorID} SubjectID={SubjectID}";
	}
}
