using System;
using System.Collections.Generic;
using System.Linq;
using Apis.Salto.Ship.Model;
using AppDriverShip.Helpers;
using AppDriverShip.Model;
using AppDriverShip.Salto;
using Prysm.AppControl.SDK.AppControlDataServiceReference;
using Prysm.AppVision.Common;
using Prysm.AppVision.Data;
using Prysm.AppVision.SDK;

namespace AppDriverShip;

internal static class SaltoResponse
{
	internal static ResponseKeyData GetResponseKeyData(UserRights userRights, string keyId, int maxValidityInHours)
	{
		int? obj;
		if (userRights == null)
		{
			obj = null;
		}
		else
		{
			Person person = userRights.Person;
			obj = ((person != null) ? new int?(person.Id) : null);
		}
		object arg = obj;
		object arg2;
		if (userRights == null)
		{
			arg2 = null;
		}
		else
		{
			Person person2 = userRights.Person;
			arg2 = ((person2 != null) ? person2.FirstName : null);
		}
		Trace.Debug($"GetResponseKeyData for {arg} ({arg2}) - timetables: {userRights?.Timetables?.Length}", "GetResponseKeyData", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoResponse.cs", 21);
		SaltoKeyData saltoKeyData = GetSaltoKeyData(userRights, maxValidityInHours);
		if (saltoKeyData == null)
		{
			return null;
		}
		return new ResponseKeyData
		{
			KeyID = keyId,
			ActionsToDo = GetActionToDo(),
			SaltoKeyData = saltoKeyData
		};
	}

	private static SaltoKeyData GetSaltoKeyData(UserRights userRights, int maxValidityInHours)
	{
		Period generalPeriod = GetGeneralPeriod(userRights.Person, maxValidityInHours);
		SaltoKeyData saltoKeyData = new SaltoKeyData
		{
			AccessRightList = GetAccessRights(userRights, maxValidityInHours),
			GeneralPeriod = generalPeriod
		};
		Person person = userRights.Person;
		if (!string.IsNullOrWhiteSpace((person != null) ? person.PinCode : null))
		{
			Person person2 = userRights.Person;
			saltoKeyData.PINCode = ((person2 != null) ? person2.PinCode : null);
			saltoKeyData.PINEnabled = 1;
		}
		saltoKeyData.UseAntipassback = (userRights.Person.IsAntipassback ? 1 : 0);
		saltoKeyData.OutputList = GetOutputList(userRights);
		saltoKeyData.Office = ((!string.IsNullOrWhiteSpace(userRights.Person.Info4) && userRights.Person.Info4 == "1") ? 1 : 0);
		saltoKeyData.Privacy = ((!string.IsNullOrWhiteSpace(userRights.Person.Info5) && userRights.Person.Info5 == "1") ? 1 : 0);
		try
		{
			if (!string.IsNullOrWhiteSpace(userRights.Person.Info6))
			{
				char[] array = userRights.Person.Info6.ToCharArray();
				saltoKeyData.LowBatteryWarning = ((array.Length != 0) ? ObjectExtensions.ToInt((object)array[0].ToString(), 0) : 0);
				saltoKeyData.AuditOpenings = ((array.Length > 1) ? ObjectExtensions.ToInt((object)array[1].ToString(), 0) : 0);
				saltoKeyData.AuditRejections = ((array.Length > 2) ? ObjectExtensions.ToInt((object)array[2].ToString(), 0) : 0);
				saltoKeyData.LastReject = ((array.Length > 3) ? ObjectExtensions.ToInt((object)array[3].ToString(), 0) : 0);
			}
		}
		catch (Exception ex)
		{
			object[] obj = new object[5]
			{
				userRights.Person.FirstName,
				userRights.Person.LastName,
				userRights.PersonId,
				null,
				null
			};
			object obj2;
			if (userRights == null)
			{
				obj2 = null;
			}
			else
			{
				Person person3 = userRights.Person;
				obj2 = ((person3 != null) ? person3.Info6 : null);
			}
			obj[3] = obj2;
			obj[4] = ex;
			Trace.Error(string.Format("GetSaltoKeyData avanced options exception for user {0} {1} ({2}): {3} > {4}", obj), "GetSaltoKeyData", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoResponse.cs", 66);
		}
		return saltoKeyData;
	}

	private static string[] GetOutputList(UserRights userRights)
	{
		List<string> list = new List<string>();
		foreach (string item2 in from x in userRights.Timetables
			where !string.IsNullOrWhiteSpace(x.Relays)
			select x.Relays)
		{
			if (item2.Contains(","))
			{
				string[] array = item2.Split(',');
				foreach (string item in array)
				{
					if (!list.Contains(item))
					{
						list.Add(item);
					}
				}
			}
			else if (!list.Contains(item2))
			{
				list.Add(item2);
			}
		}
		if (list.Any())
		{
			object arg = list.Count();
			int? obj;
			if (userRights == null)
			{
				obj = null;
			}
			else
			{
				Person person = userRights.Person;
				obj = ((person != null) ? new int?(person.Id) : null);
			}
			object arg2 = obj;
			object arg3;
			if (userRights == null)
			{
				arg3 = null;
			}
			else
			{
				Person person2 = userRights.Person;
				arg3 = ((person2 != null) ? person2.FirstName : null);
			}
			Trace.Debug($"SaltoResponse.GetOutputList - {arg} relays added for {arg2} ({arg3})", "GetOutputList", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoResponse.cs", 95);
			if (list.Count() > 130)
			{
				object arg4 = list.Count();
				int? obj2;
				if (userRights == null)
				{
					obj2 = null;
				}
				else
				{
					Person person3 = userRights.Person;
					obj2 = ((person3 != null) ? new int?(person3.Id) : null);
				}
				object arg5 = obj2;
				object arg6;
				if (userRights == null)
				{
					arg6 = null;
				}
				else
				{
					Person person4 = userRights.Person;
					arg6 = ((person4 != null) ? person4.FirstName : null);
				}
				Trace.Warning($"SaltoResponse.GetOutputList - TooManyRelays ({arg4}): should be from 1 to 128 + ESD1 and ESD2  for {arg5} ({arg6})", "GetOutputList", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoResponse.cs", 97);
			}
			return list.ToArray();
		}
		return null;
	}

	private static int GetKeyMaxValidyInDays(Person person)
	{
		int result = 2;
		if (!string.IsNullOrWhiteSpace(person.Info1))
		{
			int.TryParse(person.Info1, out result);
		}
		int days = (person.ValidityEnd - DateTime.Now).Days;
		if (days < result)
		{
			result = days;
		}
		return result;
	}

	private static AccessRight[] GetAccessRights(UserRights userRights, int maxValidityInHours)
	{
		object arg;
		if (userRights == null)
		{
			arg = null;
		}
		else
		{
			Person person = userRights.Person;
			arg = ((person != null) ? person.FirstName : null);
		}
		Trace.Debug($"GetAccessRights {arg} ({userRights?.PersonId})", "GetAccessRights", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoResponse.cs", 116);
		try
		{
			List<AccessRight> list = new List<AccessRight>();
			if (userRights?.Timetables == null)
			{
				return list.ToArray();
			}
			Trace.Debug($"timetable infos: {userRights.PersonId} ({userRights.Person.FirstName}) has {userRights?.Timetables.Length} reader rights", "GetAccessRights", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoResponse.cs", 123);
			for (int i = 0; i < userRights.Timetables.Length; i++)
			{
				if (i > 90)
				{
					Trace.Warning("************************* TOO MANY RIGHTS ****************************", "GetAccessRights", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoResponse.cs", 128);
					Trace.Warning("*************************       BREAK     ****************************", "GetAccessRights", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoResponse.cs", 129);
					break;
				}
				Timetable timeTable = userRights.Timetables[i].TimeTable;
				if (timeTable == null || !userRights.Timetables[i].ReaderName.Contains("."))
				{
					continue;
				}
				VariableRow row = PrysmSdkHelper.GetRow(userRights.Timetables[i].ReaderName);
				if (row == null)
				{
					Trace.Error("Can not find variable with name " + userRights.Timetables[i].ReaderName, "GetAccessRights", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoResponse.cs", 152);
					continue;
				}
				string parameter = Helper.GetParameter("Name", row.FullParameters, (string)null);
				string text = Helper.GetParameter("Type", row.FullParameters, (string)null) ?? Helper.GetParameter("DoorType", row.FullParameters, (string)null);
				if (text == null)
				{
					Trace.Error("Can not find doorType for door: " + row.FullAddress + " / " + ((TreeRow)row).Name, "GetAccessRights", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoResponse.cs", 164);
					continue;
				}
				int num = 2;
				string text2 = text.ToLower();
				if (!(text2 == "doors"))
				{
					if (!(text2 == "zones"))
					{
						continue;
					}
					num = 1;
				}
				else
				{
					num = 2;
				}
				DateTime dateTime = userRights.Person.ValidityStart;
				if (userRights.Timetables[i].ValidityStart.HasValue)
				{
					dateTime = userRights.Timetables[i].ValidityStart.Value;
				}
				if (dateTime < DateTime.Today)
				{
					dateTime = DateTime.Today;
				}
				if (dateTime < userRights.Person.ValidityStart)
				{
					dateTime = userRights.Person.ValidityStart;
				}
				if (dateTime > userRights.Person.ValidityEnd)
				{
					continue;
				}
				DateTime dateTime2 = userRights.Person.ValidityEnd;
				if (userRights.Timetables[i].ValidityEnd.HasValue)
				{
					dateTime2 = userRights.Timetables[i].ValidityEnd.Value;
				}
				if (dateTime2 > userRights.Person.ValidityEnd)
				{
					dateTime2 = userRights.Person.ValidityEnd;
				}
				if (!(dateTime2 < userRights.Person.ValidityStart))
				{
					if (dateTime.AddHours(maxValidityInHours) < dateTime2)
					{
						dateTime2 = dateTime.AddHours(maxValidityInHours).AddDays(1.0);
					}
					if (dateTime2 > userRights.Person.ValidityEnd)
					{
						dateTime2 = userRights.Person.ValidityEnd;
					}
					Period period = new Period
					{
						StartDate = dateTime.ToString("yyyy-MM-dd"),
						EndDate = dateTime2.ToString("yyyy-MM-dd")
					};
					AccessRight accessRight = new AccessRight
					{
						Period = period,
						AccessPoint = new AccessPoint
						{
							AccessID = parameter,
							AccessType = num
						},
						TimezoneList = Timezones.GetTimezones(timeTable, period)
					};
					if (accessRight.TimezoneList.Any())
					{
						list.Add(accessRight);
					}
				}
			}
			Trace.Debug("From timetables to salto access points: " + string.Join(" ", list.Select((AccessRight y) => y.AccessPoint)), "GetAccessRights", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoResponse.cs", 221);
			Trace.Debug("From timetables to salto periods: " + string.Join(" ", list.Select((AccessRight y) => y.Period)), "GetAccessRights", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoResponse.cs", 222);
			return list.ToArray();
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "GetAccessRights", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoResponse.cs", 227);
			return null;
		}
	}

	private static ActionToDo GetActionToDo()
	{
		return new ActionToDo
		{
			Update = 1
		};
	}

	private static Period GetGeneralPeriod(Person person, int maxValidityInHours)
	{
		if (!person.IsActive)
		{
			return new Period
			{
				StartDate = DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd"),
				EndDate = DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd")
			};
		}
		DateTime dateTime = person.ValidityStart;
		if (dateTime < DateTime.Today)
		{
			dateTime = DateTime.Today;
		}
		DateTime dateTime2 = person.ValidityEnd;
		if (dateTime.AddHours(maxValidityInHours) < dateTime2)
		{
			dateTime2 = dateTime.AddHours(maxValidityInHours).AddDays(1.0);
		}
		if (dateTime2 > person.ValidityEnd)
		{
			dateTime2 = person.ValidityEnd;
		}
		Trace.Debug(string.Format("General period for {0} ({1}) => from {2} to {3}", person.FirstName, person.Id, dateTime.ToString("yyyy-MM-dd"), dateTime2.ToString("yyyy-MM-dd")), "GetGeneralPeriod", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoResponse.cs", 266);
		return new Period
		{
			StartDate = dateTime.ToString("yyyy-MM-dd"),
			EndDate = dateTime2.ToString("yyyy-MM-dd")
		};
	}
}
