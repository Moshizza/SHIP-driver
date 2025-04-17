using System.Collections.Generic;
using System.Linq;
using Apis.Salto.Ship.Model;
using Prysm.AppVision.Data;
using Prysm.AppVision.SDK;

namespace AppDriverShip.Salto;

internal static class Timezones
{
	public static Timezone[] GetTimezones(Timetable timetable, Period generalPeriod)
	{
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		List<Timezone> list = new List<Timezone>();
		if ((((timetable != null) ? timetable.Items : null) == null || timetable == null || timetable.Items?.Any() != true) && (((timetable != null) ? timetable.DefaultItems : null) == null || timetable == null || timetable.DefaultItems?.Any() != true))
		{
			Trace.Debug("No timetable => no timezone", "GetTimezones", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Salto\\Timezones.cs", 21);
			return list.ToArray();
		}
		if (timetable.Items.Length < 1 && timetable.DefaultItems.Length != 0)
		{
			SearchAccessRightsInItems(timetable.Type, list, timetable.DefaultItems);
		}
		else
		{
			SearchAccessRightsInItems(timetable.Type, list, timetable.Items);
		}
		list = SimplifyTimezoneList(list);
		return list.ToArray();
	}

	private static void SearchAccessRightsInItems(TimetableType timetableType, List<Timezone> timezones, TimetableItem[] timePeriods)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < timePeriods.Length; i++)
		{
			if (timePeriods[i].Value == 1)
			{
				AddAuthorizedTimezone(timetableType, i, timezones, timePeriods);
			}
		}
	}

	private static List<Timezone> SimplifyTimezoneList(List<Timezone> timezones)
	{
		List<Timezone> list = new List<Timezone>();
		List<int> list2 = new List<int>();
		for (int i = 0; i < timezones.Count(); i++)
		{
			if (list2.Contains(i))
			{
				continue;
			}
			list2.Add(i);
			Timezone timezone = timezones.ToArray()[i];
			for (int j = i + 1; j < timezones.Count(); j++)
			{
				if (!list2.Contains(j) && !(timezones.ToArray()[i].StartTime != timezones.ToArray()[j].StartTime) && !(timezones.ToArray()[i].EndTime != timezones.ToArray()[j].EndTime))
				{
					timezone.DayType = timezone.DayType + " " + timezones.ToArray()[j].DayType;
					list2.Add(j);
				}
			}
			list.Add(timezone);
		}
		return list;
	}

	private static void AddAuthorizedTimezone(TimetableType timetableType, int periodIndex, List<Timezone> timezones, TimetableItem[] timePeriods)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if ((int)timetableType == 0)
		{
			AddTimezoneEachDayOfWeek(timezones, timePeriods[periodIndex], GetEndTime(timePeriods, periodIndex));
			return;
		}
		TimetableItem val = timePeriods[periodIndex];
		if (periodIndex < timePeriods.Length - 1)
		{
			TimetableItem val2 = timePeriods[periodIndex + 1];
			if (val.Day == val2.Day)
			{
				string endTime = GetEndTime(timePeriods, periodIndex);
				AddTimezone(timezones, val, endTime);
				return;
			}
			for (int i = val.Day; i <= val2.Day; i++)
			{
				string endTime2 = "23:59:59";
				if (i == val2.Day)
				{
					endTime2 = GetEndTime(timePeriods, periodIndex);
				}
				string startTime = "00:00:00";
				if (i == val.Day)
				{
					startTime = val.Hour.ToString("00") + ":" + val.Minute.ToString("00") + ":00";
				}
				string dayType = SaltoDays.GetDayType(i);
				Timezone item = new Timezone
				{
					StartTime = startTime,
					EndTime = endTime2,
					DayType = dayType
				};
				timezones.Add(item);
			}
		}
		else
		{
			string endTime3 = GetEndTime(timePeriods, periodIndex);
			AddTimezone(timezones, val, endTime3);
		}
	}

	private static void AddTimezone(List<Timezone> timezones, TimetableItem currentTimePeriod, string endTime)
	{
		Timezone timezone = CreateTimezone(currentTimePeriod, endTime, currentTimePeriod.Day);
		if (timezone != null && !timezones.Contains(timezone))
		{
			timezones.Add(timezone);
		}
	}

	private static void AddTimezoneEachDayOfWeek(List<Timezone> timezones, TimetableItem item, string endTime)
	{
		for (int i = 0; i < 7; i++)
		{
			Timezone timezone = CreateTimezone(item, endTime, i);
			if (timezone != null && !timezones.Contains(timezone))
			{
				timezones.Add(timezone);
			}
		}
	}

	private static string GetEndTime(TimetableItem[] items, int itemIndex)
	{
		string text = "23:59:59";
		if (itemIndex < items.Length - 1)
		{
			text = GetNextItemStartTime(items, itemIndex) ?? text;
		}
		return text;
	}

	private static string GetNextItemStartTime(TimetableItem[] items, int i)
	{
		_ = items[i];
		TimetableItem val = items[i + 1];
		return string.Format("{0}:{1}:00", val.Hour, val.Minute.ToString("00"));
	}

	private static Timezone CreateTimezone(TimetableItem item, string endTime, int dayIndex)
	{
		string dayType = SaltoDays.GetDayType(dayIndex);
		if (string.IsNullOrWhiteSpace(dayType))
		{
			return null;
		}
		return new Timezone
		{
			StartTime = string.Format("{0}:{1}:00", item.Hour, item.Minute.ToString("00")),
			EndTime = endTime,
			DayType = dayType
		};
	}
}
