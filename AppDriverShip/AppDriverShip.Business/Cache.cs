using System;
using System.IO;
using System.Linq;
using AppDriverShip.Helpers;
using AppDriverShip.Model;
using Newtonsoft.Json;
using Prysm.AppControl.SDK;
using Prysm.AppControl.SDK.AppControlDataServiceReference;
using Prysm.AppVision.Data;
using Prysm.AppVision.SDK;

namespace AppDriverShip.Business;

internal class Cache
{
	private VariableRow _rootVar;

	private string _basePath;

	public Cache(string basePath, VariableRow rootVar)
	{
		if (string.IsNullOrWhiteSpace((rootVar != null) ? rootVar.Address : null))
		{
			throw new ArgumentNullException("rootVar");
		}
		if (string.IsNullOrWhiteSpace(basePath))
		{
			throw new ArgumentNullException("basePath");
		}
		_basePath = basePath;
		_rootVar = rootVar;
	}

	internal bool SaveTimetableInCache(Timetable timetable, int timetableId)
	{
		try
		{
			string contents = JsonConvert.SerializeObject(timetable);
			string text = Path.Combine(_basePath, "Timetable");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			File.WriteAllText(Path.Combine(text, $"{timetableId}.txt"), contents);
			return true;
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "SaveTimetableInCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Cache.cs", 44);
			return false;
		}
	}

	internal Timetable GetTimetableFromCache(int timetableId)
	{
		string path = Path.Combine(_basePath, "Timetable", $"{timetableId}.txt");
		if (!File.Exists(path))
		{
			Timetable timetableById = PrysmSdkHelper.Datalayer.GetTimetableById(timetableId);
			SaveTimetableInCache(timetableById, timetableId);
			return timetableById;
		}
		try
		{
			return JsonConvert.DeserializeObject<Timetable>(File.ReadAllText(path));
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "GetTimetableFromCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Cache.cs", 66);
			Timetable timetableById2 = PrysmSdkHelper.Datalayer.GetTimetableById(timetableId);
			SaveTimetableInCache(timetableById2, timetableId);
			return timetableById2;
		}
	}

	internal bool SavePersonInCache(Person person)
	{
		try
		{
			if (person == null)
			{
				return false;
			}
			string contents = JsonConvert.SerializeObject(person);
			string text = Path.Combine(_basePath, "Person");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			File.WriteAllText(Path.Combine(text, $"{person.Id}.txt"), contents);
			return true;
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "SavePersonInCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Cache.cs", 91);
			return false;
		}
	}

	internal Person GetPersonFromCache(int personId)
	{
		string path = Path.Combine(_basePath, "Person", $"{personId}.txt");
		try
		{
			if (!File.Exists(path))
			{
				Person personById = PrysmSdkHelper.Datalayer.GetPersonById(personId);
				SavePersonInCache(personById);
				return personById;
			}
		}
		catch (Exception ex)
		{
			Trace.Error(ex, $"e GetPersonFromCache {personId}", "GetPersonFromCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Cache.cs", 110);
			return null;
		}
		try
		{
			return JsonConvert.DeserializeObject<Person>(File.ReadAllText(path));
		}
		catch (Exception arg)
		{
			Trace.Warning($"ex GetPersonFromCache {personId} >>> {arg}", "GetPersonFromCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Cache.cs", 122);
			try
			{
				Person personById2 = PrysmSdkHelper.Datalayer.GetPersonById(personId);
				SavePersonInCache(personById2);
				return personById2;
			}
			catch (Exception ex2)
			{
				Trace.Error(ex2, $"lex GetPersonFromCache {personId}", "GetPersonFromCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Cache.cs", 131);
				return null;
			}
		}
	}

	internal bool SaveUserRightsInCache(UserRights userRights)
	{
		try
		{
			string contents = JsonConvert.SerializeObject(userRights);
			string text = Path.Combine(_basePath, "UserRights");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			File.WriteAllText(Path.Combine(text, $"{userRights.PersonId}.txt"), contents);
			SavePersonInCache(userRights.Person);
			userRights.Timetables?.Select((UserTimetable x) => SaveTimetableInCache(x.TimeTable, x.TimetableId));
			return true;
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "SaveUserRightsInCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Cache.cs", 152);
			return false;
		}
	}

	internal UserRights GetUserRightsFromCache(int personId)
	{
		string path = Path.Combine(_basePath, "UserRights", $"{personId}.txt");
		if (!File.Exists(path))
		{
			Trace.Debug($"GetUserRightsFromCache - Cache not found. Create Cache for pid={personId}", "GetUserRightsFromCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Cache.cs", 162);
			Datalayer datalayer = PrysmSdkHelper.Datalayer;
			Person val = ((datalayer != null) ? datalayer.GetPersonById(personId) : null);
			if (val == null || val.Id != personId)
			{
				Trace.Debug($"GetUserRightsFromCache - PersonNotFound in datalayer for pid={personId}", "GetUserRightsFromCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Cache.cs", 166);
				return null;
			}
			VariableRow rootVar = _rootVar;
			UserRights result = PrysmSdkHelper.GetPersonReaderMap((rootVar != null) ? rootVar.Address : null, val).Result;
			if (result?.Person == null || result == null || result.Person.Id != personId)
			{
				object arg = personId;
				VariableRow rootVar2 = _rootVar;
				Trace.Error($"GetUserRightsFromCache => ERROR - Can not get userpersonMap for pid= {arg} with address={((rootVar2 != null) ? rootVar2.Address : null)}.", "GetUserRightsFromCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Cache.cs", 174);
				return null;
			}
			SaveUserRightsInCache(result);
			return result;
		}
		try
		{
			string value = File.ReadAllText(path);
			if (string.IsNullOrWhiteSpace(value))
			{
				Trace.Debug($"GetUserRightsFromCache => empty file for {personId}. Deleting file to recreate it", "GetUserRightsFromCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Cache.cs", 187);
				try
				{
					File.Delete(path);
				}
				catch (Exception arg2)
				{
					Trace.Error($"GetUserRightsFromCache - Can not delete empty file for pid={personId}: {arg2}", "GetUserRightsFromCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Cache.cs", 188);
				}
			}
			UserRights userRights = JsonConvert.DeserializeObject<UserRights>(value);
			if (userRights?.Person == null || userRights.PersonId != personId || userRights.Person.Id != personId)
			{
				Trace.Debug($"GetUserRightsFromCache => wrong deserialization for {personId}. Deleting file to recreate it", "GetUserRightsFromCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Cache.cs", 193);
				try
				{
					File.Delete(path);
				}
				catch (Exception arg3)
				{
					Trace.Error($"GetUserRightsFromCache - Can not delete wrong file for pid={personId}: {arg3}", "GetUserRightsFromCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Cache.cs", 194);
				}
			}
			return userRights;
		}
		catch (Exception arg4)
		{
			try
			{
				Trace.Warning($"{((TreeRow)_rootVar).Name} - GetUserRightsFromCache exception can not read cache - ex: {arg4}", "GetUserRightsFromCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Cache.cs", 202);
				Datalayer datalayer2 = PrysmSdkHelper.Datalayer;
				Person person = ((datalayer2 != null) ? datalayer2.GetPersonById(personId) : null);
				UserRights result2 = PrysmSdkHelper.GetPersonReaderMap(_rootVar.Address, person).Result;
				if (result2?.Person == null || result2 == null || result2.Person.Id != personId)
				{
					object arg5 = personId;
					VariableRow rootVar3 = _rootVar;
					Trace.Error($"GetUserRightsFromCache => EXCEPTION in Exception - Can not get userRights for pid= {arg5} with address={((rootVar3 != null) ? rootVar3.Address : null)}.", "GetUserRightsFromCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Cache.cs", 207);
					return null;
				}
				SaveUserRightsInCache(result2);
				return result2;
			}
			catch (Exception)
			{
				Trace.Error($"{((TreeRow)_rootVar).Name} - GetUserRightsFromCache exception critique e: {arg4}", "GetUserRightsFromCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Cache.cs", 215);
				return null;
			}
		}
	}
}
