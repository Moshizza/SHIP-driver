using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppDriverShip.Model;
using Prysm.AppControl.SDK;
using Prysm.AppControl.SDK.AppControlDataServiceReference;
using Prysm.AppVision.Common;
using Prysm.AppVision.Data;
using Prysm.AppVision.SDK;

namespace AppDriverShip.Helpers;

internal static class PrysmSdkHelper
{
	private static AppServerForDriver _appServer;

	private static VariableRow[] _rows;

	private static string _protocol = "";

	private static Driver _driver;

	private static ConcurrentDictionary<int, Timetable> _timetables = new ConcurrentDictionary<int, Timetable>();

	public static string ProtocolName
	{
		get
		{
			return _protocol;
		}
		set
		{
			_protocol = value;
			SetRows();
		}
	}

	public static Driver Driver
	{
		get
		{
			return _driver;
		}
		set
		{
			_driver = value;
			AppServer = _driver.AppServer;
		}
	}

	public static AppServerForDriver AppServer
	{
		get
		{
			return _appServer;
		}
		set
		{
			_appServer = value;
			AppServerForDriver appServer = _appServer;
			object protocolName;
			if (appServer == null)
			{
				protocolName = null;
			}
			else
			{
				ProtocolRow currentProtocol = appServer.CurrentProtocol;
				protocolName = ((currentProtocol != null) ? currentProtocol.Name : null);
			}
			ProtocolName = (string)protocolName;
			SetRows();
		}
	}

	public static Datalayer Datalayer
	{
		get
		{
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Expected O, but got Unknown
			if (_driver == null)
			{
				return null;
			}
			if (AppServer == null)
			{
				AppServer = _driver.AppServer;
			}
			if (string.IsNullOrWhiteSpace(_protocol))
			{
				AppServerForDriver appServer = AppServer;
				object protocolName;
				if (appServer == null)
				{
					protocolName = null;
				}
				else
				{
					ProtocolRow currentProtocol = appServer.CurrentProtocol;
					protocolName = ((currentProtocol != null) ? currentProtocol.Name : null);
				}
				ProtocolName = (string)protocolName;
			}
			return new Datalayer((AppServer)(object)_appServer);
		}
	}

	private static void SetRows()
	{
		if (_appServer == null || !((AppServer)_appServer).IsConnected || string.IsNullOrWhiteSpace(_protocol))
		{
			return;
		}
		VariableRow[] variablesByProtocol = ((AppServer)_appServer).ProtocolManager.GetVariablesByProtocol(_protocol);
		if (variablesByProtocol != null && variablesByProtocol.Any())
		{
			List<VariableRow> list = new List<VariableRow>();
			VariableRow[] array = variablesByProtocol;
			foreach (VariableRow val in array)
			{
				list.AddRange(((IRowStateManager<VariableRow, VariableState>)(object)((AppServer)_appServer).VariableManager).GetRowsByName(((TreeRow)val).Name + "*"));
			}
			_rows = list.ToArray();
		}
	}

	internal static VariableRow GetRow(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return null;
		}
		if (_rows == null || !_rows.Any())
		{
			SetRows();
		}
		return _rows.FirstOrDefault((VariableRow x) => ((TreeRow)x).Name.ToLower() == name.ToLower());
	}

	internal static async Task<UserRights> GetPersonReaderMap(string addressIn, Person person)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(addressIn) || person == null)
			{
				return null;
			}
			string[] allCardReadersNames = GetAllCardReadersNames(addressIn);
			if (allCardReadersNames == null || !allCardReadersNames.Any())
			{
				return null;
			}
			string[] allRelaysNames = GetAllRelaysNames(addressIn);
			return await GetPersonUserRights(allCardReadersNames, person, allRelaysNames);
		}
		catch (Exception ex)
		{
			object[] obj = new object[5] { addressIn, null, null, null, null };
			obj[1] = ((person != null) ? person.FirstName : null);
			obj[2] = ((person != null) ? person.LastName : null);
			obj[3] = ((person != null) ? new int?(person.Id) : null);
			obj[4] = ex;
			Trace.Error(string.Format("Exception in PrysmSdkHelper.GetPersonReaderMap with addressIn={0} and person={1}.{2}({3}) >>> {4}", obj), "GetPersonReaderMap", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Helpers\\PrysmSdkHelper.cs", 113);
			return null;
		}
	}

	internal static async Task<UserRights> GetPersonUserRights(string[] readersNames, int personId, string[] relays)
	{
		try
		{
			Person personById = Datalayer.GetPersonById(personId);
			return await GetPersonUserRights(readersNames, personById, relays);
		}
		catch (Exception arg)
		{
			Trace.Error($"Exception in PrysmSdkHelper.GetPersonReaderMap with readersNamesCount={readersNames?.Count()} and personId={personId} >>> {arg}", "GetPersonUserRights", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Helpers\\PrysmSdkHelper.cs", 126);
			return null;
		}
	}

	internal static Person GetPersonByBadgeCode(string badgeCode)
	{
		try
		{
			int personIdByBadge = Datalayer.GetPersonIdByBadge(badgeCode, "", 1);
			return Datalayer.GetPersonById(personIdByBadge);
		}
		catch (Exception arg)
		{
			Trace.Error($"GetPersonByBadgeCode ex with {badgeCode} > {arg}", "GetPersonByBadgeCode", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Helpers\\PrysmSdkHelper.cs", 141);
			return null;
		}
	}

	internal static bool StoreRomCode(int personId, string badgeCode, string romCode)
	{
		Trace.Info($"Store ROMCode for personid={personId} > {badgeCode} > {romCode}", "StoreRomCode", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Helpers\\PrysmSdkHelper.cs", 148);
		try
		{
			AppControlSdk appControlSdk = new AppControlSdk(_driver);
			Badge val = ((IEnumerable<Badge>)appControlSdk.GetBadges())?.FirstOrDefault((Badge x) => x.Code == badgeCode);
			if (val == null)
			{
				return false;
			}
			val.CustomExtension = romCode;
			appControlSdk.Save();
			return true;
		}
		catch (Exception arg)
		{
			Trace.Error($"StoreRomCode ex for {personId}/{romCode} > {arg}", "StoreRomCode", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Helpers\\PrysmSdkHelper.cs", 162);
			return false;
		}
	}

	internal static async Task<UserRights> GetPersonUserRights(string[] readersNames, Person person, string[] allRelaysNames)
	{
		Datalayer dl = Datalayer;
		int[] variableIds = GetVariableIds(readersNames);
		PersonMapRight personMap = (await dl.GetPeopleMapByReaderIds(variableIds, new int[1] { person.Id })).FirstOrDefault();
		if (personMap == null)
		{
			return null;
		}
		int[] variableIds2 = GetVariableIds(allRelaysNames);
		return CreateUserRights(personMap, readersNames, allRelaysNames, await dl.GetAllPeopleMapByReaderIds(variableIds2));
	}

	internal static async Task<List<UserRights>> GetPeopleUserRights(string[] readersNames, int[] people, string[] allRelaysNames)
	{
		Trace.Debug($"PrysmSdkHelper.GetPersonReaderMap {readersNames?.Count()} readers / {people?.Count()} persons", "GetPeopleUserRights", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Helpers\\PrysmSdkHelper.cs", 200);
		List<UserRights> users = new List<UserRights>();
		try
		{
			int[] variableIds = GetVariableIds(readersNames);
			Datalayer dl = new Datalayer((AppServer)(object)AppServer);
			PersonMapRight[] mapRights = ((!people.Any()) ? (await dl.GetAllPeopleMapByReaderIds(variableIds)) : (await dl.GetPeopleMapByReaderIds(variableIds, people)));
			int[] variableIds2 = GetVariableIds(allRelaysNames);
			PersonMapRight[] allRelayRights = await dl.GetAllPeopleMapByReaderIds(variableIds2);
			Trace.Debug("Got all rights", "GetPeopleUserRights", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Helpers\\PrysmSdkHelper.cs", 218);
			int num = 0;
			for (int i = 0; i < mapRights.Length; i++)
			{
				try
				{
					PersonMapRight rights = mapRights[i];
					if (users.Any((UserRights x) => x.PersonId == rights.Person.Id))
					{
						PersonMapRight obj = rights;
						object arg;
						if (obj == null)
						{
							arg = null;
						}
						else
						{
							Person person = obj.Person;
							arg = ((person != null) ? person.FirstName : null);
						}
						Person person2 = rights.Person;
						Trace.Warning($"USER ALREADY FOUND: {arg} (id={((person2 != null) ? new int?(person2.Id) : null)})", "GetPeopleUserRights", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Helpers\\PrysmSdkHelper.cs", 230);
					}
					UserRights item = CreateUserRights(rights, readersNames, allRelaysNames, allRelayRights);
					users.Add(item);
				}
				catch (Exception ex)
				{
					object[] obj2 = new object[7]
					{
						i,
						mapRights?.Length,
						null,
						null,
						null,
						null,
						null
					};
					PersonMapRight obj3 = mapRights[i];
					int? obj4;
					if (obj3 == null)
					{
						obj4 = null;
					}
					else
					{
						Person person3 = obj3.Person;
						obj4 = ((person3 != null) ? new int?(person3.Id) : null);
					}
					obj2[2] = obj4;
					PersonMapRight obj5 = mapRights[i];
					object obj6;
					if (obj5 == null)
					{
						obj6 = null;
					}
					else
					{
						Person person4 = obj5.Person;
						obj6 = ((person4 != null) ? person4.FirstName : null);
					}
					obj2[3] = obj6;
					obj2[4] = readersNames?.Count();
					obj2[5] = people?.Count();
					obj2[6] = ex;
					Trace.Error(string.Format("PrysmSdkHelper.GetPersonReaderMap Exception for rights r={0}/{1} with pid={2} ({3})    >>{4} readers / {5} persons >>> {6}", obj2), "GetPeopleUserRights", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Helpers\\PrysmSdkHelper.cs", 237);
					num++;
				}
			}
			if (num > 0)
			{
				throw new Exception($"Errors found update users: {num}");
			}
			return users;
		}
		catch (Exception arg2)
		{
			Trace.Error($"PrysmSdkHelper.GetPeopleUserRights exception with {readersNames?.Count()} readers and {people?.Count()} persons    >>>  {arg2}", "GetPeopleUserRights", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Helpers\\PrysmSdkHelper.cs", 246);
			throw;
		}
	}

	public static UserRights CreateUserRights(PersonMapRight rights, string[] _readerNames, string[] relays, PersonMapRight[] allRelayRights)
	{
		try
		{
			if (rights != null)
			{
				Person person = rights.Person;
				if (person != null)
				{
					_ = person.Id;
					if (0 == 0)
					{
						string[] relayRightsForPerson = GetRelayRightsForPerson(rights, relays, allRelayRights);
						UserRights userRights = new UserRights();
						userRights.PersonId = rights.Person.Id;
						userRights.Person = rights.Person;
						userRights.Timetables = new List<UserTimetable>().ToArray();
						List<UserTimetable> list = new List<UserTimetable>();
						if (userRights.Timetables != null && userRights.Timetables.Any())
						{
							list.AddRange(userRights.Timetables);
						}
						int i;
						for (i = 0; i < rights.TimetableMap.Length; i++)
						{
							TimetableAndValidity val = rights.TimetableMap[i];
							if (val.TimetableId == 0)
							{
								continue;
							}
							Timetable timetableById = GetTimetableById(val.TimetableId);
							UserTimetable userTimetable = new UserTimetable();
							userTimetable.ReaderName = _readerNames[i];
							userTimetable.TimetableId = val.TimetableId;
							userTimetable.TimeTable = timetableById;
							userTimetable.ValidityStart = rights.Person.ValidityStart;
							userTimetable.ValidityEnd = rights.Person.ValidityEnd;
							if (_rows.FirstOrDefault((VariableRow x) => ((x != null) ? ((TreeRow)x).Name : null) == _readerNames[i]) != null)
							{
								string[] array = MapRelaysToReader(relayRightsForPerson, new string[1] { _readerNames[i] });
								if (array != null && array.Any())
								{
									string text = string.Join(",", array);
									if (!string.IsNullOrWhiteSpace(text))
									{
										userTimetable.Relays = text;
									}
								}
							}
							list.Add(userTimetable);
						}
						userRights.Timetables = list.ToArray();
						return userRights;
					}
				}
			}
			return null;
		}
		catch (Exception arg)
		{
			Trace.Error($"PrysmSdkHelper.CreateUserRights ex: {arg}", "CreateUserRights", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Helpers\\PrysmSdkHelper.cs", 307);
			return null;
		}
	}

	private static string[] GetRelayRightsForPerson(PersonMapRight readersRights, string[] allRelaysNames, PersonMapRight[] allRelayRights)
	{
		PersonMapRight val = allRelayRights.FirstOrDefault((PersonMapRight x) => x.Person.Id == readersRights.Person.Id);
		if (val != null && val != null)
		{
			Person person = val.Person;
			if (person != null)
			{
				_ = person.Id;
				if (0 == 0 && ((val != null) ? val.TimetableMap : null) != null && val.TimetableMap.Any())
				{
					List<string> list = new List<string>();
					for (int i = 0; i < val.TimetableMap.Length; i++)
					{
						if (val.TimetableMap[i].TimetableId >= 1)
						{
							list.Add(allRelaysNames[i]);
						}
					}
					return list.ToArray();
				}
			}
		}
		return new string[0];
	}

	private static string[] MapRelaysToReader(string[] allRelays, string[] readersNamesWithRights)
	{
		if (_rows == null)
		{
			return new string[0];
		}
		List<string> list = new List<string>();
		foreach (string reader in readersNamesWithRights)
		{
			VariableRow val = _rows.FirstOrDefault((VariableRow x) => ((TreeRow)x).Name == reader);
			string readerAddress = val.FullAddress;
			foreach (string relay in allRelays)
			{
				VariableRow val2 = _rows.FirstOrDefault((VariableRow x) => ((TreeRow)x).Name == relay && x.FullAddress == readerAddress);
				if (val2 != null)
				{
					string parameter = Helper.GetParameter("Relays", val2.FullParameters, (string)null);
					if (!string.IsNullOrWhiteSpace(parameter))
					{
						list.Add(parameter);
					}
				}
			}
		}
		return list.ToArray();
	}

	public static int[] GetVariableIds(string[] variableNames)
	{
		if (!((AppServer)_appServer).IsConnected)
		{
			Trace.Error("Lost AppServer connection. Restart.", "GetVariableIds", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Helpers\\PrysmSdkHelper.cs", 360);
			_driver.Stop();
			_driver.Start();
		}
		_ = Datalayer;
		List<int> list = new List<int>();
		foreach (string name in variableNames)
		{
			VariableRow val = _rows.FirstOrDefault((VariableRow x) => ((TreeRow)x).Name == name);
			if (val != null)
			{
				list.Add(((TreeRow)val).Id);
				continue;
			}
			val = ((IRowStateManager<VariableRow, VariableState>)(object)((AppServer)_appServer).VariableManager).GetRowByName(name);
			if (val != null)
			{
				list.Add(((TreeRow)val).Id);
				continue;
			}
			Trace.Debug("GetVariableIds can not find row : " + name, "GetVariableIds", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Helpers\\PrysmSdkHelper.cs", 383);
			list.Add(0);
		}
		return list.ToArray();
	}

	public static Timetable GetTimetableById(int id)
	{
		Timetable val = null;
		if (_timetables.ContainsKey(id))
		{
			val = _timetables[id];
			if (val != null)
			{
				return val;
			}
		}
		val = Datalayer.GetTimetableById(id);
		_timetables[id] = val;
		return val;
	}

	public static void ClearTimetables()
	{
		_timetables.Clear();
	}

	internal static Timetable[] GetTimetableArray(string[] readersNames, PersonMapRight map)
	{
		Timetable[] array = (Timetable[])(object)new Timetable[readersNames.Length];
		Datalayer datalayer = Datalayer;
		for (int i = 0; i < map.TimetableMap.Length; i++)
		{
			TimetableAndValidity val = map.TimetableMap[i];
			Timetable timetableById = datalayer.GetTimetableById(val.TimetableId);
			if (timetableById == null)
			{
				array[i] = null;
			}
			else
			{
				array[i] = timetableById;
			}
		}
		return array;
	}

	internal static string[] GetAllCardReadersNames(string _addressIn)
	{
		List<string> list = new List<string>();
		try
		{
			VariableRow rootRow = _rows.FirstOrDefault((VariableRow x) => x.FullAddress == _addressIn && !((TreeRow)x).Name.Contains("."));
			if (rootRow == null)
			{
				return list.ToArray();
			}
			return (from x in _rows
				where ((TreeRow)x).Name.ToLower().EndsWith(".reader") && ((TreeRow)x).Name.StartsWith(((TreeRow)rootRow).Name)
				select ((TreeRow)x).Name).ToArray();
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "GetAllCardReadersNames", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Helpers\\PrysmSdkHelper.cs", 445);
		}
		return list.ToArray();
	}

	internal static string[] GetAllRelaysNames(string _addressIn)
	{
		List<string> list = new List<string>();
		try
		{
			VariableRow rootRow = _rows.FirstOrDefault((VariableRow x) => x.FullAddress == _addressIn && !((TreeRow)x).Name.Contains("."));
			if (rootRow == null)
			{
				return list.ToArray();
			}
			return (from x in _rows
				where ((TreeRow)x).Name.ToLower().Contains(".relay") && ((TreeRow)x).Name.StartsWith(((TreeRow)rootRow).Name)
				select ((TreeRow)x).Name).ToArray();
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "GetAllRelaysNames", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Helpers\\PrysmSdkHelper.cs", 464);
		}
		return list.ToArray();
	}

	internal static bool PersonIsAuthorized(PersonMapRight personAndTimetables)
	{
		if (((personAndTimetables != null) ? personAndTimetables.Person : null) == null)
		{
			return false;
		}
		if (!personAndTimetables.Person.IsActive)
		{
			return false;
		}
		if (personAndTimetables.Person.ValidityStart > DateTime.Now)
		{
			return false;
		}
		return true;
	}
}
