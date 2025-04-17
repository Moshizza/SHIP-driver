using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Apis.Salto.Ship.Model;
using AppDriverShip.Ship;
using Prysm.AppVision.Common;
using Prysm.AppVision.SDK;
using Salto.Salto.Ship.Helpers;
using Salto.Salto.Ship.Model;

namespace Apis.Salto.Ship.Api;

public class SaltoDb_OnlineDoors
{
	private ShipTransmitter _shipTransmitter;

	internal SaltoDb_OnlineDoors(ShipTransmitter shipTransmitter)
	{
		if (shipTransmitter == null)
		{
			throw new ArgumentNullException("SaltoDb - shipTransmitter can't be null");
		}
		_shipTransmitter = shipTransmitter;
	}

	public async Task<string> SaltoDBUpdate(object o, string method = "Update")
	{
		if (string.IsNullOrWhiteSpace(method))
		{
			throw new ArgumentNullException("SaltoDBUpdate method can't be null");
		}
		if (o == null)
		{
			throw new ArgumentNullException("SaltoDBUpdate o can't be null");
		}
		try
		{
			string prm = DataSerializer<object>.Serializer(o);
			string text = await _shipTransmitter.SendRequest(o.GetType().Name + "." + method, prm);
			if (string.IsNullOrWhiteSpace(text))
			{
				return "-1";
			}
			string text2 = o.GetType().Name;
			if (text2.Contains("SaltoDB"))
			{
				text2 = text2.Replace("SaltoDB", "");
				text2 += "ID";
			}
			string text3 = StringExtensions.Between(text, "<" + text2 + ">", "</" + text2 + ">");
			if (string.IsNullOrWhiteSpace(text3))
			{
				text2 = XmlHelpers.FindIdMarkup(text);
				text3 = StringExtensions.Between(text, "<" + text2 + ">", "</" + text2 + ">");
			}
			return text3;
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "SaltoDBUpdate", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\SaltoDb_OnlineDoors.cs", 69);
			throw;
		}
	}

	public async Task<SaltoDBTimezoneTable[]> SaltoDBTimezoneTableListRead(int maxCount, int startIndex = 0)
	{
		if (maxCount < 1)
		{
			maxCount = int.MaxValue;
		}
		string arg = "";
		if (startIndex > 1)
		{
			arg = $"<StartingFromTimezoneTableID>{startIndex}</StartingFromTimezoneTableID>";
		}
		string prm = $"<MaxCount>{maxCount}</MaxCount>{arg}";
		return DataSerializer<SaltoDBTimezoneTable>.DeserializeReadResponseArray(XmlHelpers.ResponseReadToXmlObject(await _shipTransmitter.SendRequest("SaltoDBTimezoneTableList.Read", prm), "SaltoDBTimezoneTableList"));
	}

	public async Task<int> SaltoDBTimezoneTableUpdate(SaltoDBTimezoneTable timezoneTable)
	{
		if (timezoneTable == null)
		{
			return -2;
		}
		string text = await SaltoDBUpdate(timezoneTable);
		if (int.TryParse(text, out var result))
		{
			return result;
		}
		Trace.Debug("result NOK: " + text, "SaltoDBTimezoneTableUpdate", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\SaltoDb_OnlineDoors.cs", 110);
		return -3;
	}

	public async Task<SaltoDBTimedPeriodTable[]> SaltoDBTimedPeriodTableListRead(int maxCount, int startIndex = 0)
	{
		if (maxCount < 1)
		{
			maxCount = int.MaxValue;
		}
		string arg = "";
		if (startIndex > 1)
		{
			arg = $"<StartingFromTimedPeriodTableID>{startIndex}</StartingFromTimedPeriodTableID>";
		}
		string prm = $"<MaxCount>{maxCount}</MaxCount>{arg}";
		return DataSerializer<SaltoDBTimedPeriodTable>.DeserializeReadResponseArray(XmlHelpers.ResponseReadToXmlObject(await _shipTransmitter.SendRequest("SaltoDBTimedPeriodTableList.Read", prm), "SaltoDBTimedPeriodTableList"));
	}

	public async Task<int> SaltoDBTimedPeriodTableUpdate(SaltoDBTimedPeriodTable timedPeriodTable)
	{
		if (timedPeriodTable == null)
		{
			return -1;
		}
		string text = await SaltoDBUpdate(timedPeriodTable);
		if (int.TryParse(text, out var result))
		{
			return result;
		}
		Trace.Debug("result NOK: " + text, "SaltoDBTimedPeriodTableUpdate", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\SaltoDb_OnlineDoors.cs", 148);
		return -3;
	}

	public async Task<string> SaltoDBDoorInsert(SaltoDBDoor door)
	{
		if (door == null)
		{
			return "-1";
		}
		return await SaltoDBUpdate(door, "Insert");
	}

	public async Task<string> SaltoDBDoorUpdate(SaltoDBDoor door)
	{
		if (door == null)
		{
			return "-1";
		}
		return await SaltoDBUpdate(door);
	}

	public async Task<string> SaltoDBDoorDelete(string extDoorID)
	{
		if (string.IsNullOrWhiteSpace(extDoorID))
		{
			throw new ArgumentNullException("extDoorID");
		}
		string prm = "<ExtDoorID>" + extDoorID + "</ExtDoorID>";
		return StringExtensions.Between(await _shipTransmitter.SendRequest("SaltoDBDoor.Delete", prm), "<ExtDoorID>", "</ExtDoorID>");
	}

	public async Task<OnlineDoorStatus[]> GetOnlineDoorStatus()
	{
		string prm = "<ExtDoorIDList/>";
		string text = await _shipTransmitter.SendRequest("OnlineDoor.GetOnlineStatusList", prm);
		if (string.IsNullOrWhiteSpace(text))
		{
			return new OnlineDoorStatus[0];
		}
		string text2 = XmlHelpers.ResponseReadToXmlObject(text, "OnlineDoorStatusList");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return new OnlineDoorStatus[0];
		}
		return DataSerializer<OnlineDoorStatus>.DeserializeReadResponseArray(text2);
	}

	public async Task<OnlineDoorStatus[]> GetOnlineDoorStatusByName()
	{
		string prm = "<DoorNameList/>";
		string text = await _shipTransmitter.SendRequest("OnlineDoor.GetOnlineStatusList", prm);
		if (string.IsNullOrWhiteSpace(text))
		{
			return new OnlineDoorStatus[0];
		}
		string text2 = XmlHelpers.ResponseReadToXmlObject(text, "OnlineDoorStatusList");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return new OnlineDoorStatus[0];
		}
		return DataSerializer<OnlineDoorStatus>.DeserializeReadResponseArray(text2);
	}

	public async Task<bool> CmdOnlineDoorOpen(string doorId)
	{
		return await CmdOnlineDoor("OnlineDoor.Open", doorId);
	}

	public async Task<bool> CmdOnlineDoorOpen(string[] extDoorIds)
	{
		return await CmdOnlineDoor("OnlineDoor.Open", extDoorIds);
	}

	public async Task<bool> CmdOnlineDoorClose(string doorId)
	{
		return await CmdOnlineDoor("OnlineDoor.Close", doorId);
	}

	public async Task<bool> CmdOnlineDoorClose(string[] extDoorIds)
	{
		return await CmdOnlineDoor("OnlineDoor.Close", extDoorIds);
	}

	public async Task<bool> CmdOnlineDoorEmergencyOpen(string doorId)
	{
		return await CmdOnlineDoor("OnlineDoor.EmergencyOpen", doorId);
	}

	public async Task<bool> CmdOnlineDoorEmergencyOpen(string[] extDoorIds)
	{
		return await CmdOnlineDoor("OnlineDoor.EmergencyOpen", extDoorIds);
	}

	public async Task<bool> CmdOnlineDoorEmergencyClose(string doorId)
	{
		return await CmdOnlineDoor("OnlineDoor.EmergencyClose", doorId);
	}

	public async Task<bool> CmdOnlineDoorEmergencyClose(string[] extDoorIds)
	{
		return await CmdOnlineDoor("OnlineDoor.EmergencyClose", extDoorIds);
	}

	public async Task<bool> CmdOnlineDoorEndOfEmergency(string doorId)
	{
		return await CmdOnlineDoor("OnlineDoor.EndOfEmergency", doorId);
	}

	public async Task<bool> CmdOnlineDoorEndOfEmergency(string[] extDoorIds)
	{
		return await CmdOnlineDoor("OnlineDoor.EndOfEmergency", extDoorIds);
	}

	public async Task<bool> CmdOnlineDoorStartOfficeMode(string doorId)
	{
		return await CmdOnlineDoor("OnlineDoor.StartOfficeMode", doorId);
	}

	public async Task<bool> CmdOnlineDoorStartOfficeMode(string[] extDoorIds)
	{
		return await CmdOnlineDoor("OnlineDoor.StartOfficeMode", extDoorIds);
	}

	public async Task<bool> CmdOnlineDoorEndOfficeMode(string doorId)
	{
		return await CmdOnlineDoor("OnlineDoor.EndOfficeMode", doorId);
	}

	public async Task<bool> CmdOnlineDoorEndOfficeMode(string[] extDoorIds)
	{
		return await CmdOnlineDoor("OnlineDoor.EndOfficeMode", extDoorIds);
	}

	private async Task<bool> CmdOnlineDoor(string commandName, string doorId)
	{
		string text = "<ExtDoorIDList>\r\n";
		text = text + "<ExtDoorID>" + doorId + "</ExtDoorID>\r\n";
		text += "</ExtDoorIDList>\r\n";
		return await ExecuteCmdOnlineDoor(commandName, text);
	}

	private async Task<bool> CmdOnlineDoor(string commandName, string[] extOnlineDoorIds)
	{
		string text = "<ExtDoorIDList>\r\n";
		foreach (string text2 in extOnlineDoorIds)
		{
			text = text + "<ExtDoorID>" + text2 + "</ExtDoorID>\r\n";
		}
		text += "</ExtDoorIDList>\r\n";
		return await ExecuteCmdOnlineDoor(commandName, text);
	}

	public async Task<bool> CmdOnlineRelayByName(string[] relays, bool value = true)
	{
		string text = "<RelayNameList>\r\n";
		foreach (string text2 in relays)
		{
			text = text + "<RelayName>" + text2 + "</RelayName>\r\n";
		}
		text += "</RelayNameList>\r\n";
		if (value)
		{
			text += "<ActivationTime>0</ActivationTime>\r\n";
		}
		string commandName = (value ? "OnlineRelay.Activate" : "OnlineRelay.Deactivate");
		return await ExecuteCmdOnlineDoor(commandName, text);
	}

	public async Task<bool> CmdOnlineRelayById(string[] relays, bool value = true)
	{
		string text = "<RelayNameList>\r\n";
		foreach (string text2 in relays)
		{
			text = text + "<RelayName>" + text2 + "</RelayName>\r\n";
		}
		text += "</RelayNameList>\r\n";
		if (value)
		{
			text += "<ActivationTime>0</ActivationTime>\r\n";
		}
		string commandName = (value ? "OnlineRelay.Activate" : "OnlineRelay.Deactivate");
		return await ExecuteCmdOnlineDoor(commandName, text);
	}

	public async Task<bool> CmdOnlineDoorOpenByName(string doorName)
	{
		return await CmdOnlineDoorByName("OnlineDoor.Open", doorName);
	}

	public async Task<bool> CmdOnlineDoorOpenByName(string[] doorNames)
	{
		return await CmdOnlineDoorByName("OnlineDoor.Open", doorNames);
	}

	public async Task<bool> CmdOnlineDoorCloseByName(string doorName)
	{
		return await CmdOnlineDoorByName("OnlineDoor.Close", doorName);
	}

	public async Task<bool> CmdOnlineDoorCloseByName(string[] doorNames)
	{
		return await CmdOnlineDoorByName("OnlineDoor.Close", doorNames);
	}

	public async Task<bool> CmdOnlineDoorEmergencyOpenByName(string doorName)
	{
		return await CmdOnlineDoorByName("OnlineDoor.EmergencyOpen", doorName);
	}

	public async Task<bool> CmdOnlineDoorEmergencyOpenByName(string[] doorNames)
	{
		return await CmdOnlineDoorByName("OnlineDoor.EmergencyOpen", doorNames);
	}

	public async Task<bool> CmdOnlineDoorEmergencyCloseByName(string doorName)
	{
		return await CmdOnlineDoorByName("OnlineDoor.EmergencyClose", doorName);
	}

	public async Task<bool> CmdOnlineDoorEmergencyCloseByName(string[] doorNames)
	{
		return await CmdOnlineDoorByName("OnlineDoor.EmergencyClose", doorNames);
	}

	public async Task<bool> CmdOnlineDoorEndOfEmergencyByName(string doorName)
	{
		return await CmdOnlineDoorByName("OnlineDoor.EndOfEmergency", doorName);
	}

	public async Task<bool> CmdOnlineDoorEndOfEmergencyByName(string[] doorNames)
	{
		return await CmdOnlineDoorByName("OnlineDoor.EndOfEmergency", doorNames);
	}

	public async Task<bool> CmdOnlineDoorStartOfficeModeByName(string doorName)
	{
		return await CmdOnlineDoorByName("OnlineDoor.StartOfficeMode", doorName);
	}

	public async Task<bool> CmdOnlineDoorStartOfficeModeByName(string[] doorNames)
	{
		return await CmdOnlineDoorByName("OnlineDoor.StartOfficeMode", doorNames);
	}

	public async Task<bool> CmdOnlineDoorEndOfficeModeByName(string doorName)
	{
		return await CmdOnlineDoorByName("OnlineDoor.EndOfficeMode", doorName);
	}

	public async Task<bool> CmdOnlineDoorEndOfficeModeByName(string[] doorNames)
	{
		return await CmdOnlineDoorByName("OnlineDoor.EndOfficeMode", doorNames);
	}

	private async Task<bool> CmdOnlineDoorByName(string commandName, string doorName)
	{
		string text = "<DoorNameList>\r\n";
		text = text + "<DoorID>" + doorName + "</DoorID>\r\n";
		text += "</DoorNameList>\r\n";
		return await ExecuteCmdOnlineDoor(commandName, text);
	}

	private async Task<bool> CmdOnlineDoorByName(string commandName, string[] doorNames)
	{
		string text = "<DoorNameList>\r\n";
		foreach (string text2 in doorNames)
		{
			text = text + "<DoorID>" + text2 + "</DoorID>\r\n";
		}
		text += "</DoorNameList>\r\n";
		return await ExecuteCmdOnlineDoor(commandName, text);
	}

	private async Task<bool> ExecuteCmdOnlineDoor(string commandName, string parameters)
	{
		try
		{
			string text = await _shipTransmitter.SendRequest(commandName, parameters);
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			string text2 = XmlHelpers.ResponseReadToXmlObject(text, commandName);
			if (string.IsNullOrWhiteSpace(text2))
			{
				return false;
			}
			if (string.IsNullOrWhiteSpace(StringExtensions.Between(text2, "<ArrayOf" + commandName + ">", "</ArrayOf" + commandName + ">")))
			{
				return true;
			}
			OnlineDoorActionResult[] array = DataSerializer<OnlineDoorActionResult>.DeserializeReadResponseArray(text2);
			if (array == null)
			{
				return false;
			}
			return !array.Any((OnlineDoorActionResult x) => x == null || x.ErrorCode != 0);
		}
		catch (Exception)
		{
			return false;
		}
	}

	public async Task<SaltoDBDoor[]> SaltoDBDoorListRead(int maxCount, int startIndex = 0, bool returnMembershipDoorZone = false, bool returnMembershipDoorLocation = false, bool returnMembershipDoorFunction = false)
	{
		if (maxCount < 1 || maxCount > 10000)
		{
			maxCount = 10000;
		}
		string arg = "";
		if (startIndex > 1)
		{
			arg = $"<StartingFromExtDoorID>{startIndex}</StartingFromExtDoorID>";
		}
		string text = "";
		if (returnMembershipDoorZone)
		{
			text += "<ReturnMembership_Door_Zone>1</ReturnMembership_Door_Zone>";
		}
		if (returnMembershipDoorLocation)
		{
			text += "<ReturnMembership_Door_Location>1</ReturnMembership_Door_Location>";
		}
		if (returnMembershipDoorFunction)
		{
			text += "<ReturnMembership_Door_Function>1</ReturnMembership_Door_Function>";
		}
		string prm = $"<MaxCount>{maxCount}</MaxCount>{arg}{text}";
		string text2 = await _shipTransmitter.SendRequest("SaltoDBDoorList.Read", prm);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return new SaltoDBDoor[0];
		}
		string text3 = XmlHelpers.ResponseReadToXmlObject(text2, "SaltoDBDoorList");
		if (string.IsNullOrWhiteSpace(text3))
		{
			return new SaltoDBDoor[0];
		}
		return DataSerializer<SaltoDBDoor>.DeserializeReadResponseArray(text3);
	}

	public async Task<SaltoDBLocker[]> SaltoDBLockerListRead(int maxCount, int startIndex = 0)
	{
		if (maxCount < 1 || maxCount > 10000)
		{
			maxCount = 10000;
		}
		string arg = "";
		if (startIndex > 1)
		{
			arg = $"<StartingFromExtDoorID>{startIndex}</StartingFromExtDoorID>";
		}
		string arg2 = "";
		string prm = $"<MaxCount>{maxCount}</MaxCount>{arg}{arg2}";
		string text = await _shipTransmitter.SendRequest("SaltoDBLockerList.Read", prm);
		if (string.IsNullOrWhiteSpace(text))
		{
			return new SaltoDBLocker[0];
		}
		string text2 = XmlHelpers.ResponseReadToXmlObject(text, "SaltoDBLockerList");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return new SaltoDBLocker[0];
		}
		return DataSerializer<SaltoDBLocker>.DeserializeReadResponseArray(text2);
	}

	public async Task<SaltoDBHotelRoom[]> SaltoDBHotelRoomListRead(int maxCount, int startIndex = 0)
	{
		if (maxCount < 1 || maxCount > 10000)
		{
			maxCount = 10000;
		}
		string arg = "";
		if (startIndex > 1)
		{
			arg = $"<StartingFromExtDoorID>{startIndex}</StartingFromExtDoorID>";
		}
		string arg2 = "";
		string prm = $"<MaxCount>{maxCount}</MaxCount>{arg}{arg2}";
		string text = await _shipTransmitter.SendRequest("SaltoDBHotelRoomList.Read", prm);
		if (string.IsNullOrWhiteSpace(text))
		{
			return new SaltoDBHotelRoom[0];
		}
		string text2 = XmlHelpers.ResponseReadToXmlObject(text, "SaltoDBHotelRoomList");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return new SaltoDBHotelRoom[0];
		}
		return DataSerializer<SaltoDBHotelRoom>.DeserializeReadResponseArray(text2);
	}

	public async Task<SaltoDBHotelSuite[]> SaltoDBHotelSuiteListRead(int maxCount, int startIndex = 0)
	{
		if (maxCount < 1 || maxCount > 10000)
		{
			maxCount = 10000;
		}
		string arg = "";
		if (startIndex > 1)
		{
			arg = $"<StartingFromExtDoorID>{startIndex}</StartingFromExtDoorID>";
		}
		string arg2 = "";
		string prm = $"<MaxCount>{maxCount}</MaxCount>{arg}{arg2}";
		string text = await _shipTransmitter.SendRequest("SaltoDBHotelSuiteList.Read", prm);
		if (string.IsNullOrWhiteSpace(text))
		{
			return new SaltoDBHotelSuite[0];
		}
		string text2 = XmlHelpers.ResponseReadToXmlObject(text, "SaltoDBHotelSuiteList");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return new SaltoDBHotelSuite[0];
		}
		return DataSerializer<SaltoDBHotelSuite>.DeserializeReadResponseArray(text2);
	}

	public async Task<string> SaltoDBZoneInsertOrUpdate(SaltoDBZone zone)
	{
		if (zone == null)
		{
			return "-1";
		}
		return await SaltoDBUpdate(zone, "InsertOrUpdate");
	}

	public async Task<string> SaltoDBZoneDelete(string extZoneID)
	{
		if (string.IsNullOrWhiteSpace(extZoneID))
		{
			throw new ArgumentNullException("extZoneID");
		}
		string prm = "<ExtZoneID>" + extZoneID + "</ExtZoneID>";
		return StringExtensions.Between(await _shipTransmitter.SendRequest("SaltoDBZone.Delete", prm), "<ExtZoneID>", "</ExtZoneID>");
	}

	public async Task<SaltoDBZone[]> SaltoDBZoneListRead(int maxCount, int startIndex = 0, bool returnMembershipDoorZone = true)
	{
		if (maxCount < 1 || maxCount > 10000)
		{
			maxCount = 10000;
		}
		string arg = "";
		if (startIndex > 1)
		{
			arg = $"<StartingFromExtZoneID>{startIndex}</StartingFromExtZoneID>";
		}
		string text = "";
		if (returnMembershipDoorZone)
		{
			text += "<ReturnMembership_Door_Zone>1</ReturnMembership_Door_Zone>";
		}
		string prm = $"<MaxCount>{maxCount}</MaxCount>{arg}{text}";
		string text2 = await _shipTransmitter.SendRequest("SaltoDBZoneList.Read", prm);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return new SaltoDBZone[0];
		}
		string text3 = XmlHelpers.ResponseReadToXmlObject(text2, "SaltoDBZoneList");
		if (string.IsNullOrWhiteSpace(text3))
		{
			return new SaltoDBZone[0];
		}
		return DataSerializer<SaltoDBZone>.DeserializeReadResponseArray(text3);
	}

	public async Task<SaltoDBOnlineRelay[]> SaltoDBOnlineRelayListRead(int maxCount, int startIndex = 0)
	{
		if (maxCount < 1 || maxCount > 10000)
		{
			maxCount = 10000;
		}
		string arg = "";
		if (startIndex > 1)
		{
			arg = $"<StartingFromOnlineRelayID>{startIndex}</StartingFromOnlineRelayID>";
		}
		string arg2 = "";
		string prm = $"<MaxCount>{maxCount}</MaxCount>{arg}{arg2}";
		string text = await _shipTransmitter.SendRequest("SaltoDBOnlineRelayList.Read", prm);
		if (string.IsNullOrWhiteSpace(text))
		{
			return new SaltoDBOnlineRelay[0];
		}
		string text2 = XmlHelpers.ResponseReadToXmlObject(text, "SaltoDBOnlineRelayList");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return new SaltoDBOnlineRelay[0];
		}
		return DataSerializer<SaltoDBOnlineRelay>.DeserializeReadResponseArray(text2);
	}

	public async Task<SaltoDBOnlineInput[]> SaltoDBOnlineInputListRead(int maxCount, int startIndex = 0)
	{
		if (maxCount < 1 || maxCount > 10000)
		{
			maxCount = 10000;
		}
		string arg = "";
		if (startIndex > 1)
		{
			arg = $"<StartingFromOnlineInputID>{startIndex}</StartingFromOnlineInputID>";
		}
		string arg2 = "";
		string prm = $"<MaxCount>{maxCount}</MaxCount>{arg}{arg2}";
		string text = await _shipTransmitter.SendRequest("SaltoDBOnlineInputList.Read", prm);
		if (string.IsNullOrWhiteSpace(text))
		{
			return new SaltoDBOnlineInput[0];
		}
		string text2 = XmlHelpers.ResponseReadToXmlObject(text, "SaltoDBOnlineInputList");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return new SaltoDBOnlineInput[0];
		}
		return DataSerializer<SaltoDBOnlineInput>.DeserializeReadResponseArray(text2);
	}

	public async Task<SaltoDBAuditTrailEvent[]> SaltoDBAuditTrailRead(int startEventID, int count, int descending, int showDoorId = 0, int showUserId = 0)
	{
		if (count > 10000)
		{
			count = 10000;
		}
		string prm = $"<MaxCount>{count}</MaxCount><StartingFromEventID>{startEventID}</StartingFromEventID><ShowDoorIDAs>{showDoorId}</ShowDoorIDAs><ShowUserIDAs>{showUserId}</ShowUserIDAs><DescendingOrder>{descending}</DescendingOrder>";
		string text = await _shipTransmitter.SendRequest("SaltoDBAuditTrail.Read", prm);
		try
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			string s = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\" ?><ArrayOfSaltoDBAuditTrailEvent>" + StringExtensions.Between(text, "<SaltoDBAuditTrail>", "</SaltoDBAuditTrail>") + "</ArrayOfSaltoDBAuditTrailEvent>";
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(SaltoDBAuditTrailEvent[]));
			using StringReader textReader = new StringReader(s);
			return xmlSerializer.Deserialize(textReader) as SaltoDBAuditTrailEvent[];
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "SaltoDBAuditTrailRead", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\SaltoDb_OnlineDoors.cs", 710);
			return null;
		}
	}
}
