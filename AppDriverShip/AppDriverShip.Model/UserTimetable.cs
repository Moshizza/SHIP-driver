using System;
using Prysm.AppVision.Data;

namespace AppDriverShip.Model;

internal class UserTimetable
{
	public Timetable TimeTable { get; set; }

	public int TimetableId { get; set; }

	public DateTime? ValidityStart { get; set; }

	public DateTime? ValidityEnd { get; set; }

	public string ReaderName { get; set; }

	public string Relays { get; set; }
}
