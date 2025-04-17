using System;
using System.Threading.Tasks;
using Apis.Salto.Ship.Model;
using AppDriverShip.Ship;
using Prysm.AppVision.Common;
using Prysm.AppVision.SDK;

namespace Apis.Salto.Ship.Api;

public class ApiInfo
{
	private readonly ShipTransmitter _shipTransmitter;

	internal ApiInfo(ShipTransmitter shipTransmitter)
	{
		if (shipTransmitter == null)
		{
			throw new ArgumentNullException("shipTransmitter");
		}
		_shipTransmitter = shipTransmitter;
	}

	public async Task<Info> GetInfoAsync()
	{
		string text = await _shipTransmitter.SendRequest("GetInfo");
		if (string.IsNullOrWhiteSpace(text))
		{
			Trace.Debug("ApiInfo.GetInfoAsync no salto info", "GetInfoAsync", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\ApiInfo.cs", 32);
			return null;
		}
		string text2 = StringExtensions.Between(text, "<Params>", "</Params>");
		if (string.IsNullOrWhiteSpace(text2))
		{
			Trace.Debug("ApiInfo.GetInfoAsync no salto xmlParams", "GetInfoAsync", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\ApiInfo.cs", 38);
			return null;
		}
		return new Info
		{
			ProtocolID = StringExtensions.Between(text2, "<ProtocolID>", "</ProtocolID>")?.Trim(),
			ProtocolVersion = StringExtensions.Between(text2, "<ProtocolVersion>", "</ProtocolVersion>")?.Trim(),
			DateTime = StringExtensions.Between(text2, "<DateTime>", "</DateTime>")?.Trim(),
			DefaultLanguage = StringExtensions.Between(text2, "<DefaultLanguage>", "</DefaultLanguage>")?.Trim()
		};
	}
}
