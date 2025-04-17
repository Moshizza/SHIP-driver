using System;

namespace AppDriverShip.Model;

internal class EventCard
{
	public int EventID { get; set; }

	public string DoorID { get; set; }

	public string SubjectID { get; set; }

	public int Operation { get; set; }

	public DateTimeOffset DateUtc { get; set; }

	public override string ToString()
	{
		return $"SaltoEvent: EventID={EventID} DoorID={DoorID} SubjectID={SubjectID}";
	}
}
