using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Apis.Salto.Ship;
using Apis.Salto.Ship.Model;
using AppDriverShip.Helpers;
using AppDriverShip.Model;
using Prysm.AppVision.Common;
using Prysm.AppVision.Data;
using Prysm.AppVision.SDK;

namespace AppDriverShip.Bridge;

internal class SwitchGetSaltoBinaries : ISwitchStrategy
{
	private readonly ShipApi _ship;

	private readonly AppServerForDriver _appServer;

	private readonly string _addressIn;

	private readonly string _hostname;

	private readonly string _user;

	private readonly string _password;

	private string _csn = "";

	private readonly VariableRow[] _rows;

	private int _desfireFileSize;

	private int _maxValidityInHours;

	public SwitchGetSaltoBinaries(ShipApi ship, AppServerForDriver appServer, string addressIn)
	{
		if (ship == null)
		{
			throw new ArgumentNullException("ship");
		}
		if (appServer == null)
		{
			throw new ArgumentNullException("appServer");
		}
		if (string.IsNullOrWhiteSpace(addressIn))
		{
			throw new ArgumentOutOfRangeException("addressIn");
		}
		_ship = ship;
		_appServer = appServer;
		_addressIn = addressIn;
		Trace.Debug("SaltoBinaries ctor", ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 44);
		string parameters = GetParameters();
		_hostname = Helper.GetParameter("DatawriterHost", parameters, (string)null);
		_user = Helper.GetParameter("DatawriterUser", parameters, (string)null);
		_password = Helper.GetParameter("DatawriterPassword", parameters, (string)null);
		_maxValidityInHours = Helper.GetParameter<int>("MaxValidityInHours", parameters, 720);
		_desfireFileSize = Helper.GetParameter<int>("DesfireFileSize", parameters, 2048);
		Trace.Debug("SaltoBinaries get rows", ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 53);
		_rows = ((IRowStateManager<VariableRow, VariableState>)(object)((AppServer)_appServer).VariableManager).GetRows();
		Trace.Debug($"SaltoBinaries ctor ok with {_rows?.Count()} rows", ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 55);
	}

	private string GetParameters()
	{
		IEnumerable<VariableRow> enumerable = from x in ((IRowStateManager<VariableRow, VariableState>)(object)((AppServer)_appServer).VariableManager).GetRowsByName("*.Comm")
			where x.FullAddress != null && x.FullAddress.ToLower().Contains(_addressIn.ToLower())
			select x;
		if (enumerable == null)
		{
			return null;
		}
		VariableRow obj = enumerable.FirstOrDefault();
		if (obj == null)
		{
			return null;
		}
		return obj.FullParameters;
	}

	public string DoConditionnal(HttpListenerRequest request)
	{
		throw new NotImplementedException();
	}

	public string DoConditionnal(string parameters, HttpListenerRequest request)
	{
		try
		{
			Trace.Debug("Bridge: GetSaltoBinaries => " + parameters, "DoConditionnal", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 81);
			string[] allCardReadersNames = PrysmSdkHelper.GetAllCardReadersNames(_addressIn);
			if (allCardReadersNames == null || !allCardReadersNames.Any())
			{
				return "Error connecting AppControl";
			}
			Trace.Debug("SwitchGetSaltoBinaries - GotReader names. Go for person Id " + parameters, "DoConditionnal", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 86);
			string badgeCode = GetBadgeCode(parameters);
			int personIdByBadge = PrysmSdkHelper.Datalayer.GetPersonIdByBadge(badgeCode, "", 1);
			if (personIdByBadge < 1)
			{
				Trace.Debug("Can't find personId for this Badge: " + badgeCode + " - Badge format must be empty and type = 0", "DoConditionnal", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 92);
				return "Error in parameters. Badge format must be empty. Check the logs";
			}
			Trace.Debug($"SwitchGetSaltoBinaries - GetBinaries for {badgeCode} / pid={personIdByBadge}", "DoConditionnal", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 96);
			EditNewKeyBinaryRequest binaryRequest = GetBinaryRequest(allCardReadersNames, personIdByBadge, badgeCode);
			if (binaryRequest == null)
			{
				return "Error computing BinaryRequest";
			}
			EditNewKeyBinaryResponse binaryResponse = SaltoSdkHelper.GetBinaryResponse(_ship, binaryRequest);
			if (binaryResponse == null)
			{
				return "Salto return an error. See log file";
			}
			if (binaryResponse is EditNewKeyBinaryResponseDesfire)
			{
				if (!(binaryResponse is EditNewKeyBinaryResponseDesfire editNewKeyBinaryResponseDesfire))
				{
					return "error parsing response to desfire format";
				}
				Trace.Debug("SwitchGetSaltoBinaries: SaltoResponse = " + editNewKeyBinaryResponseDesfire.KeyID + " with binaries", "DoConditionnal", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 111);
				return editNewKeyBinaryResponseDesfire?.DesfireBinaryData?.Files?.FirstOrDefault()?.BinaryData;
			}
			if (binaryResponse is EditNewKeyBinaryResponseMifare)
			{
				if (binaryResponse is EditNewKeyBinaryResponseMifare editNewKeyBinaryResponseMifare)
				{
					return editNewKeyBinaryResponseMifare?.MifareBinaryData?.BlockBinaryData?.FirstOrDefault()?.BinaryData;
				}
				return "error parsing response to mifare format";
			}
			return "Salto response is not Desfire and can not be defined";
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "DoConditionnal", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 128);
			return "SaltoBinaryException - " + ex.Message;
		}
	}

	private string GetBadgeCode(string parameters)
	{
		if (string.IsNullOrWhiteSpace(parameters))
		{
			Trace.Error("Error: can not find keyid or personid in request", "GetBadgeCode", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 136);
		}
		if (!parameters.Contains("/"))
		{
			Trace.Error("Error while trying to get SwitchGetSaltoBinaries Request - wrong format", "GetBadgeCode", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 139);
		}
		string[] array = parameters.Split('/');
		if (array.Length < 3)
		{
			Trace.Error("Error while trying to get SwitchGetSaltoBinaries Request - missing arguments", "GetBadgeCode", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 143);
		}
		_csn = array[2];
		if (string.IsNullOrWhiteSpace(_csn) || (_csn.Length != 8 && _csn.Length != 14))
		{
			Trace.Error("Error - wrong _csn format " + _csn, "GetBadgeCode", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 147);
		}
		return array[3];
	}

	private EditNewKeyBinaryRequest GetBinaryRequest(string[] readersNames, int personId, string keyId)
	{
		Trace.Debug($"SwitchGetSaltoBinaries.GetBinaryRequest for {personId} with key {keyId} for {readersNames.Length} readers", "GetBinaryRequest", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 154);
		try
		{
			Trace.Debug("SwitchGetSaltoBinaries.GetBinaryRequest connected to sdk. Search for personTimetables", "GetBinaryRequest", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 162);
			if (!readersNames.Any())
			{
				Trace.Debug("SwitchGetSaltoBinaries.GetBinaryRequest - no access for this user", "GetBinaryRequest", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 166);
				throw new IndexOutOfRangeException("Can not find any access for this user");
			}
			string[] allRelaysNames = PrysmSdkHelper.GetAllRelaysNames(_addressIn);
			UserRights result = PrysmSdkHelper.GetPersonUserRights(readersNames, personId, allRelaysNames).Result;
			if (result == null || result.Timetables == null || !result.Timetables.Any())
			{
				Trace.Debug("SwitchGetSaltoBinaries.GetBinaryRequest - No timetable for this user", "GetBinaryRequest", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 182);
				return null;
			}
			Trace.Debug("SwitchGetSaltoBinaries.GetBinaryRequest - prepare binary request", "GetBinaryRequest", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 188);
			UserTimetable[] array = result?.Timetables;
			foreach (UserTimetable userTimetable in array)
			{
				object[] obj = new object[4] { userTimetable.ReaderName, userTimetable.ValidityStart, userTimetable.ValidityEnd, null };
				int? obj2;
				if (userTimetable == null)
				{
					obj2 = null;
				}
				else
				{
					Timetable timeTable = userTimetable.TimeTable;
					obj2 = ((timeTable != null) ? timeTable.Items?.Length : null);
				}
				obj[3] = obj2;
				Trace.Debug(string.Format("userTimetime tt: {0} => {1} to {2} / {3}", obj), "GetBinaryRequest", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 191);
			}
			ResponseKeyData responseKeyData = SaltoResponse.GetResponseKeyData(result, keyId, _maxValidityInHours);
			if (responseKeyData?.SaltoKeyData == null)
			{
				Trace.Debug("SwitchGetSaltoBinaries.GetBinaryRequest - No salto key data", "GetBinaryRequest", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 198);
				return null;
			}
			Trace.Debug($"SwitchGetSaltoBinaries.GetBinaryRequest - salto key Data = {responseKeyData.KeyID} / {responseKeyData.ActionsToDo?.Update} / access rights = {responseKeyData.SaltoKeyData?.AccessRightList?.Length} ", "GetBinaryRequest", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 202);
			AccessRight[] accessRightList = responseKeyData.SaltoKeyData.AccessRightList;
			foreach (AccessRight accessRight in accessRightList)
			{
				Trace.Debug($"AccessRight: {accessRight.AccessPoint.AccessID}:{accessRight.AccessPoint.AccessType} / start:{accessRight.Period.StartDate}/ end:{accessRight.Period.EndDate}", "GetBinaryRequest", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 204);
			}
			EditNewKeyBinaryRequestDesfire binaryRequestDesfire = SaltoSdkHelper.GetBinaryRequestDesfire(keyId, _csn, responseKeyData, _desfireFileSize);
			Trace.Debug($"SwitchGetSaltoBinaries.GetBinaryRequest - salto binary request = {binaryRequestDesfire.DesfireStructure.DesfireCardSerialNumber}/ files:{binaryRequestDesfire.DesfireStructure.Files.Count} /{binaryRequestDesfire.SaltoKeyData.GeneralPeriod.StartDate}", "GetBinaryRequest", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 207);
			return binaryRequestDesfire;
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "GetBinaryRequest", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchGetSaltoBinaries.cs", 212);
			return null;
		}
	}
}
