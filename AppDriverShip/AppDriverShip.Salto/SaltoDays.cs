using System.Collections.Generic;

namespace AppDriverShip.Salto;

internal static class SaltoDays
{
	private static readonly Dictionary<int, string> days;

	static SaltoDays()
	{
		days = new Dictionary<int, string>();
		days.Add(0, "SUN");
		days.Add(1, "MON");
		days.Add(2, "TUE");
		days.Add(3, "WED");
		days.Add(4, "THU");
		days.Add(5, "FRI");
		days.Add(6, "SAT");
		days.Add(7, "HOL");
	}

	internal static string GetDayType(int dayIndex)
	{
		if (!days.ContainsKey(dayIndex))
		{
			return "";
		}
		return days[dayIndex];
	}
}
