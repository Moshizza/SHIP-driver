using System;
using Apis.Messaging;
using Prysm.AppVision.Common;
using Prysm.AppVision.SDK;

namespace Apis.Salto.Ship.Host;

internal class HostApi : IChildMessager<string>
{
	private IParentMessager<string> _driver;

	public string ApiInfo
	{
		get
		{
			string parameters = "<ProtocolID>SHIP</ProtocolID><ProtocolVersion>1.2</ProtocolVersion><DateTime>" + DateTime.Now.ToString() + "</DateTime><DefaultLanguage>eng</DefaultLanguage>";
			string text = ResponseFormatter("GetInfo", parameters);
			Trace.Debug("HostApi.ApiInfo: " + text, "ApiInfo", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Host\\HostApi.cs", 37);
			return text;
		}
	}

	public HostApi(IParentMessager<string> parent)
	{
		if (parent == null)
		{
			throw new ArgumentNullException("parent");
		}
		_driver = parent;
	}

	public void RegisterParentClass(IParentMessager<string> parent)
	{
		if (parent == null)
		{
			throw new ArgumentNullException("parent");
		}
		_driver = parent;
	}

	internal string GetKeyDataForNewKey(string frame)
	{
		string result = "";
		if (!TryGetKey(this, frame, "ROMCode", out result))
		{
			return result;
		}
		string result2 = "";
		if (!TryGetResponseKeyData("[NEWKEY]" + result, out result2))
		{
			return GetCancelKey(result);
		}
		return ResponseFormatter("GetKeyDataForNewKey", result2);
	}

	internal string GetKeyData(string frame)
	{
		Trace.Debug("HostApi.GetKeyData enter", "GetKeyData", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Host\\HostApi.cs", 59);
		string result = "";
		if (!TryGetKey(this, frame, "KeyID", out result))
		{
			return result;
		}
		string result2 = "";
		if (!TryGetResponseKeyData(result, out result2))
		{
			return GetCancelKey(result);
		}
		return ResponseFormatter("GetKeyData", result2);
	}

	private bool TryGetResponseKeyData(string keyId, out string result)
	{
		if (string.IsNullOrWhiteSpace(keyId))
		{
			Trace.Error("HostApi.TryGetResponseKeyData ERROR missing parameter keyId", "TryGetResponseKeyData", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Host\\HostApi.cs", 77);
			result = "HostApi.TryGetResponseKeyData ERROR missing parameter keyId";
			return false;
		}
		if (_driver == null)
		{
			Trace.Error("HostApi.TryGetResponseKeyData ERROR can not connect to appControl", "TryGetResponseKeyData", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Host\\HostApi.cs", 83);
			result = "HostApi.TryGetResponseKeyData ERROR can not connect to appControl";
			return false;
		}
		string text = _driver.PassMessage(keyId);
		if (string.IsNullOrWhiteSpace(text) || text == "2")
		{
			Trace.Debug("TryGetResponseKeyData return cancelKey for " + keyId + " => serialized from driver : " + text, "TryGetResponseKeyData", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Host\\HostApi.cs", 94);
			result = "cancel";
			return false;
		}
		result = StringExtensions.Between(text, "<ResponseKeyData>", "</ResponseKeyData>");
		if (!string.IsNullOrWhiteSpace(result))
		{
			Trace.Debug("TryGetResponseKeyData ok for " + keyId, "TryGetResponseKeyData", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Host\\HostApi.cs", 101);
			return true;
		}
		result = "HostApi.TryGetResponseKeyData ERROR no valid response received for " + keyId + ": " + text;
		Trace.Debug(result, "TryGetResponseKeyData", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Host\\HostApi.cs", 106);
		return false;
	}

	private string GetCancelKey(string keyId)
	{
		Trace.Debug("HostApi.GetCancelKey: " + keyId, "GetCancelKey", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Host\\HostApi.cs", 112);
		string parameters = "<KeyID>" + keyId + "</KeyID>";
		return ResponseFormatter("CancelKey", parameters);
	}

	private static bool TryGetKey(HostApi instance, string frame, string keyname, out string result)
	{
		result = "";
		if (string.IsNullOrWhiteSpace(frame))
		{
			throw new ArgumentNullException("frame");
		}
		result = StringExtensions.Between(frame, "<" + keyname + ">", "</" + keyname + ">");
		if (string.IsNullOrWhiteSpace(result))
		{
			result = "Missing parameter: KeyId is required";
			Trace.Debug("HostApi.TryGetKey => " + result + " // keyName = " + keyname + " // frame=" + frame, "TryGetKey", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Host\\HostApi.cs", 131);
			return false;
		}
		return true;
	}

	private static string ResponseFormatter(string method, string parameters)
	{
		return "<?xml version=\"1.0\" encoding=\"UTF-8\"?><RequestResponse><RequestName>" + method + "</RequestName><Params>" + parameters + "</Params></RequestResponse>";
	}
}
