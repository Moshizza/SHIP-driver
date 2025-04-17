using System;
using System.Threading.Tasks;
using Apis.Salto.Ship.Model;
using AppDriverShip.Ship;
using Prysm.AppVision.Common;
using Prysm.AppVision.SDK;

namespace Apis.Salto.Ship.Api;

public class Encoder
{
	private readonly ShipTransmitter _shipTransmitter;

	internal Encoder(ShipTransmitter shipTransmitter)
	{
		if (shipTransmitter == null)
		{
			throw new ArgumentNullException("shipTransmitter");
		}
		_shipTransmitter = shipTransmitter;
	}

	public void CancelKey(string keyId)
	{
		if (string.IsNullOrWhiteSpace(keyId))
		{
			return;
		}
		CancelKeyRequest request = new CancelKeyRequest
		{
			KeyID = keyId
		};
		try
		{
			Task.Run(delegate
			{
				try
				{
					string text = DataSerializer<CancelKeyRequest>.Serializer(request);
					if (!string.IsNullOrWhiteSpace(text))
					{
						string text2 = StringExtensions.Between(text, "<CancelKeyRequest>", "</CancelKeyRequest>");
						if (string.IsNullOrWhiteSpace(text2))
						{
							Trace.Debug("ERROR: can not find cancelKey params", "CancelKey", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\Encoder.cs", 50);
						}
						else if (string.IsNullOrWhiteSpace(_shipTransmitter.SendRequest("CancelKey", text2).Result))
						{
							Trace.Debug("ERROR: Salto didn't respond anything for CancelKey", "CancelKey", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\Encoder.cs", 58);
						}
					}
				}
				catch (Exception ex2)
				{
					Trace.Error(ex2, null, "CancelKey", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\Encoder.cs", 64);
				}
			});
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "CancelKey", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\Encoder.cs", 70);
		}
	}

	public async Task<EditNewKeyResponse> EditNewKey(EditNewKeyRequest request)
	{
		if (request == null)
		{
			throw new ArgumentNullException("request");
		}
		try
		{
			string text = DataSerializer<EditNewKeyRequest>.Serializer(request);
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			string text2 = StringExtensions.Between(text, "<EditNewKeyRequest>", "</EditNewKeyRequest>");
			if (string.IsNullOrWhiteSpace(text2))
			{
				Trace.Debug("ERROR: can not find binaryrequest card type", "EditNewKey", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\Encoder.cs", 96);
				return null;
			}
			string text3 = await _shipTransmitter.SendRequest("Encoder.EditNewKey", text2);
			if (string.IsNullOrWhiteSpace(text3))
			{
				Trace.Debug("ERROR: Salto didn't respond anything", "EditNewKey", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\Encoder.cs", 104);
				return null;
			}
			string text4 = StringExtensions.Between(text3, "<KeyID>", "</KeyID>");
			if (!string.IsNullOrWhiteSpace(text4) && request.ReturnKeyID == 1 && text4 != request.KeyID)
			{
				Trace.Debug("ERROR: Salto returned keyId is not valid", "EditNewKey", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\Encoder.cs", 110);
				return null;
			}
			string text5 = StringExtensions.Between(text3, "<Exception>", "</Exception>");
			if (!string.IsNullOrWhiteSpace(text5))
			{
				string text6 = StringExtensions.Between(text5, "<Code>", "</Code>");
				string text7 = StringExtensions.Between(text5, "<Message>", "</Message>");
				Trace.Error("Encoder.EditNewKey ERROR on " + request.EncoderID + " Code: " + text6 + " / Message: " + text7, "EditNewKey", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\Encoder.cs", 119);
				return null;
			}
			string text8 = StringExtensions.Between(text3, "<Params>", "</Params>");
			if (!string.IsNullOrWhiteSpace(text8))
			{
				text8 = "<EditNewKeyResponse>" + text8 + "</EditNewKeyResponse>";
				return DataSerializer<EditNewKeyResponse>.DeserializeReadResponse(text8);
			}
			return null;
		}
		catch (Exception ex)
		{
			Trace.Error($"Encoder.EditNewKey Exception on {request?.EncoderID} with keyId={request?.KeyID} has {request?.SaltoKeyData?.AccessRightList?.Length} rights   >>> {ex}", "EditNewKey", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\Encoder.cs", 133);
			return null;
		}
	}

	public async Task<EditNewKeyBinaryResponse> GetBinaryDataForNewKey(EditNewKeyBinaryRequest binaryRequest)
	{
		if (binaryRequest == null)
		{
			throw new ArgumentNullException("binaryRequest");
		}
		string binaryRequestParameters = GetBinaryRequestParameters(binaryRequest);
		if (string.IsNullOrWhiteSpace(binaryRequestParameters))
		{
			Trace.Debug("ERROR: can not find binaryrequest card type", "GetBinaryDataForNewKey", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\Encoder.cs", 153);
			return null;
		}
		string text = await _shipTransmitter.SendRequest("Encoder.EditNewKey.GetBinaryData", binaryRequestParameters);
		if (string.IsNullOrWhiteSpace(text))
		{
			Trace.Debug("ERROR: Salto didn't respond anything", "GetBinaryDataForNewKey", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\Encoder.cs", 161);
			return null;
		}
		string text2 = StringExtensions.Between(text, "<KeyID>", "</KeyID>");
		if (!string.IsNullOrWhiteSpace(text2) && binaryRequest.ReturnKeyID == 1 && text2 != binaryRequest.KeyID)
		{
			Trace.Debug("ERROR: Salto returned keyId is not valid", "GetBinaryDataForNewKey", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\Encoder.cs", 167);
			return null;
		}
		string text3 = StringExtensions.Between(text, "<Exception>", "</Exception>");
		if (!string.IsNullOrWhiteSpace(text3))
		{
			string text4 = StringExtensions.Between(text3, "<Code>", "</Code>");
			string text5 = StringExtensions.Between(text3, "<Message>", "</Message>");
			Trace.Error("Encoder.GetBinaryDataForNewKey ERROR Code: " + text4 + " / Message: " + text5, "GetBinaryDataForNewKey", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\Encoder.cs", 176);
			return null;
		}
		try
		{
			string text6 = StringExtensions.Between(text, "<Params>", "</Params>");
			if (!string.IsNullOrWhiteSpace(text6))
			{
				string text7 = FindKeyTypeName(text6);
				text6 = "<" + text7 + ">" + text6 + "</" + text7 + ">";
				if (text7 == "EditNewKeyBinaryResponseDesfire")
				{
					return DataSerializer<EditNewKeyBinaryResponseDesfire>.DeserializeReadResponse(text6);
				}
				if (text7 == "EditNewKeyBinaryResponseMifare")
				{
					return DataSerializer<EditNewKeyBinaryResponseMifare>.DeserializeReadResponse(text6);
				}
			}
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "GetBinaryDataForNewKey", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\Encoder.cs", 200);
		}
		return null;
	}

	private string GetBinaryRequestParameters(EditNewKeyBinaryRequest binaryRequest)
	{
		string result = "";
		if (binaryRequest is EditNewKeyBinaryRequestDesfire)
		{
			string text = DataSerializer<EditNewKeyBinaryRequestDesfire>.Serializer((EditNewKeyBinaryRequestDesfire)binaryRequest);
			if (!string.IsNullOrWhiteSpace(text))
			{
				result = StringExtensions.Between(text, "<EditNewKeyBinaryRequestDesfire>", "</EditNewKeyBinaryRequestDesfire>");
			}
		}
		if (binaryRequest is EditNewKeyBinaryRequestMifare)
		{
			string text2 = DataSerializer<EditNewKeyBinaryRequestMifare>.Serializer((EditNewKeyBinaryRequestMifare)binaryRequest);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				result = StringExtensions.Between(text2, "<EditNewKeyBinaryRequestMifare>", "</EditNewKeyBinaryRequestMifare>");
			}
		}
		return result;
	}

	private string FindKeyTypeName(string response)
	{
		if (response.ToLower().Contains("desfire"))
		{
			return "EditNewKeyBinaryResponseDesfire";
		}
		if (response.ToLower().Contains("mifare"))
		{
			return "EditNewKeyBinaryResponseMifare";
		}
		return "UnknownType";
	}

	private string GetFakeDesfireResponse()
	{
		return string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>" + "<RequestResponse>", "<RequestName>Encoder.EditNewKey.GetBinaryData</RequestName>"), "<Params>"), "<KeyID>21</KeyID>"), "<DesfireBinaryData>"), "<FileBinaryDataList>"), "<FileBinaryData>"), "<FileID>1</FileID>"), "<StartAddress>0</StartAddress>"), "<BinaryData>a1252154a1151e12f4512ef</BinaryData>"), "</FileBinaryData>"), "</FileBinaryDataList>"), "</DesfireBinaryData>"), "</Params>"), "</RequestResponse>");
	}

	private string GetFakeMifareResponse()
	{
		return string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>" + "<RequestResponse>", "<RequestName>Encoder.EditNewKey.GetBinaryData</RequestName>"), "<Params>"), "<KeyID>21</KeyID>"), "<MifareBinaryData>"), "<BlockBinaryDataList>"), "<BlockBinaryData>"), "<SectorID>13</SectorID>"), "<BlockID>0</BlockID>"), "<BinaryData>a1252154a1151e12f4512ef</BinaryData>"), "</BlockBinaryData>"), "<BlockBinaryData>"), "<SectorID>13</SectorID>"), "<BlockID>1</BlockID>"), "<BinaryData>a1252154a1151e12f4512ef</BinaryData>"), "</BlockBinaryData>"), "<BlockBinaryData>"), "<SectorID>14</SectorID>"), "<BlockID>0</BlockID>"), "<BinaryData>a1252154a1151e12f4512ef</BinaryData>"), "</BlockBinaryData>"), "</BlockBinaryDataList>"), "</MifareBinaryData>"), "</Params>"), "</RequestResponse>");
	}
}
