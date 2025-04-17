using Prysm.AppControl.SDK.AppControlDataServiceReference;

namespace AppDriverShip.Model;

internal class UserRights
{
	public int PersonId { get; set; }

	public Person Person { get; set; }

	public UserTimetable[] Timetables { get; set; }
}
