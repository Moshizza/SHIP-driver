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

internal class SwitchDirectSaltoEncoder : ISwitchStrategy
{
	private readonly ShipApi _ship;

	private readonly AppServerForDriver _appServer;

	private readonly string _addressIn;

	private readonly string _hostname;

	private readonly string _user;

	private readonly string _password;

	private string _encoder = "";

	private int _maxValidityInHours;

	private List<VariableRow> _rows = new List<VariableRow>();

	private Driver _driver;

	public SwitchDirectSaltoEncoder(ShipApi ship, Driver driver, string addressIn)
	{
		if (ship == null)
		{
			throw new ArgumentNullException("ship");
		}
		if (driver?.AppServer == null)
		{
			throw new ArgumentNullException("SwtichDirectSaltoEncoder, driver can not be null");
		}
		if (string.IsNullOrWhiteSpace(addressIn))
		{
			throw new ArgumentOutOfRangeException("addressIn");
		}
		Trace.Info("Direct encoder ctor", ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 40);
		_driver = driver;
		_ship = ship;
		_appServer = driver.AppServer;
		_addressIn = addressIn;
		Trace.Debug("Direct encoder get rows", ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 45);
		_rows = ((IRowStateManager<VariableRow, VariableState>)(object)((AppServer)_appServer).VariableManager).GetRows().ToList();
		Trace.Debug($"Direct encoder get rows found {_rows.Count()} rows", ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 47);
		string parameters = GetParameters();
		Trace.Debug("Reader params found: " + parameters, ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 50);
		_hostname = Helper.GetParameter("DatawriterHost", parameters, (string)null);
		_user = Helper.GetParameter("DatawriterUser", parameters, (string)null);
		_password = Helper.GetParameter("DatawriterPassword", parameters, (string)null);
		_maxValidityInHours = Helper.GetParameter<int>("MaxValidityInHours", parameters, 720);
		Trace.Info("Direct encoder ctor ok", ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 55);
	}

	private string GetParameters()
	{
		try
		{
			AppServerForDriver appServer = _appServer;
			object result;
			if (appServer == null)
			{
				result = null;
			}
			else
			{
				IVariableManager variableManager = ((AppServer)appServer).VariableManager;
				if (variableManager == null)
				{
					result = null;
				}
				else
				{
					VariableRow[] rows = ((IRowStateManager<VariableRow, VariableState>)(object)variableManager).GetRows();
					if (rows == null)
					{
						result = null;
					}
					else
					{
						VariableRow obj = rows.FirstOrDefault((VariableRow x) => x.Address != null && x.Address.ToLower().Contains(_addressIn?.Trim()?.ToLower()));
						result = ((obj != null) ? obj.FullParameters : null);
					}
				}
			}
			return (string)result;
		}
		catch (Exception)
		{
			Trace.Debug("Can not found reader params > seach row with address= " + _addressIn + " ", "GetParameters", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 68);
			return "";
		}
	}

	public string DoConditionnal(HttpListenerRequest request)
	{
		throw new NotImplementedException();
	}

	public string DoConditionnal(string parameters, HttpListenerRequest request)
	{
		PrysmSdkHelper.Driver = _driver;
		PrysmSdkHelper.ProtocolName = _appServer.CurrentProtocol.Name;
		string[] allCardReadersNames = PrysmSdkHelper.GetAllCardReadersNames(_addressIn);
		if (allCardReadersNames == null || !allCardReadersNames.Any())
		{
			return "Error connecting AppControl";
		}
		Trace.Debug("SwitchDirectSaltoEncoder - GotReader names. Go for person Id " + parameters, "DoConditionnal", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 88);
		string badgeCode = GetBadgeCode(parameters);
		int personIdByBadge = PrysmSdkHelper.Datalayer.GetPersonIdByBadge(badgeCode, "", 1);
		if (personIdByBadge < 1)
		{
			personIdByBadge = PrysmSdkHelper.Datalayer.GetPersonIdByBadge(badgeCode, "", 0);
			if (personIdByBadge < 1)
			{
				Trace.Debug("Error in parameters. Can't find the person by BadgeCode. Format must be emptt and type = 1 - Badge Code =" + badgeCode, "DoConditionnal", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 97);
				return "Error in parameters. Can't find the person by BadgeCode. Format must be empty. Check the logs";
			}
		}
		Trace.Debug("SwitchDirectSaltoEncoder - ProceedEncoding for " + badgeCode, "DoConditionnal", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 102);
		string text = ProceedEncoding(allCardReadersNames, personIdByBadge, badgeCode);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "Error encoding";
		}
		return text;
	}

	private string GetBadgeCode(string parameters)
	{
		if (string.IsNullOrWhiteSpace(parameters))
		{
			Trace.Error("Error: can not find keyid or personid in request", "GetBadgeCode", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 113);
		}
		if (!parameters.Contains("/"))
		{
			Trace.Error("Error while trying to get SwitchDirectSaltoEncoder Request - wrong format", "GetBadgeCode", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 116);
		}
		string[] array = parameters.Split('/');
		_encoder = array[1];
		if (string.IsNullOrWhiteSpace(_encoder))
		{
			Trace.Error("Error - wrong encoder name", "GetBadgeCode", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 122);
		}
		return array[0];
	}

	private string ProceedEncoding(string[] readersNames, int personId, string keyId)
	{
		if (!readersNames.Any())
		{
			return null;
		}
		string[] allRelaysNames = PrysmSdkHelper.GetAllRelaysNames(_addressIn);
		UserRights result = PrysmSdkHelper.GetPersonUserRights(readersNames, personId, allRelaysNames).Result;
		if (result == null || result.Timetables == null || !result.Timetables.Any())
		{
			Trace.Debug("SwitchGetSaltoBinaries.GetBinaryRequest - No timetable", "ProceedEncoding", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 139);
			return null;
		}
		Trace.Debug("SwitchGetSaltoBinaries.GetBinaryRequest - prepare binary request", "ProceedEncoding", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 145);
		ResponseKeyData responseKeyData = SaltoResponse.GetResponseKeyData(result, keyId, _maxValidityInHours);
		if (responseKeyData?.SaltoKeyData == null)
		{
			Trace.Error($"SaltoKeyData is null for {personId}/{keyId}", "ProceedEncoding", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 149);
			return null;
		}
		Trace.Debug("GetNewKeyRequest for " + keyId, "ProceedEncoding", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 168);
		EditNewKeyRequest newKeyRequest = SaltoSdkHelper.GetNewKeyRequest(keyId, _encoder, responseKeyData.SaltoKeyData);
		Trace.Debug($"Encode {keyId} with keyData= {responseKeyData.ActionsToDo} > {responseKeyData.SaltoKeyData.AccessRightList.Length} rights like accessid={responseKeyData.SaltoKeyData.AccessRightList.FirstOrDefault()?.AccessPoint.AccessID}", "ProceedEncoding", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 170);
		EditNewKeyResponse result2 = _ship.Encoder.EditNewKey(newKeyRequest).Result;
		if (result2 == null || result2.KeyID == null || result2 == null || result2.ROMCode == null)
		{
			Trace.Error("Invalid encoder response " + result2?.KeyID + " should be " + keyId + " and " + result2?.ROMCode + " not null", "ProceedEncoding", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 174);
			return null;
		}
		if (result2.KeyID != newKeyRequest.KeyID)
		{
			Trace.Error("Invalid encoder response " + result2?.KeyID + " should be " + keyId + " and " + result2?.ROMCode + " not null", "ProceedEncoding", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 180);
			return null;
		}
		Trace.Debug($"EditNewKey > Valid response received from encoder for personId={personId} KeyID={result2.KeyID} - ROMCode={result2.ROMCode}", "ProceedEncoding", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 183);
		PrysmSdkHelper.StoreRomCode(personId, keyId, result2.ROMCode);
		return result2.ROMCode;
	}

	private int GetPersonId(string parameters)
	{
		if (string.IsNullOrWhiteSpace(parameters))
		{
			Trace.Error("Error: can not find keyid or personid in request", "GetPersonId", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 191);
		}
		if (!parameters.Contains("/"))
		{
			Trace.Error("Error while trying to get SwitchGetSaltoBinaries Request - wrong format", "GetPersonId", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 194);
		}
		string[] array = parameters.Split('/');
		if (array.Length < 3)
		{
			Trace.Error("Error while trying to get SwitchGetSaltoBinaries Request - missing arguments", "GetPersonId", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 198);
		}
		_encoder = array[1];
		if (string.IsNullOrWhiteSpace(_encoder))
		{
			Trace.Error("Error - wrong encoder name", "GetPersonId", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 202);
		}
		int result = 0;
		if (!int.TryParse(array[0], out result))
		{
			Trace.Error("Error getting personId parameter", "GetPersonId", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchDirectSaltoEncoder.cs", 206);
		}
		return result;
	}
}
