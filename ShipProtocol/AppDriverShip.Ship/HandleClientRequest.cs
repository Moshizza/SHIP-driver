using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Apis.Salto.Ship.Communication;
using Apis.Salto.Ship.Host;
using Prysm.AppVision.SDK;

namespace AppDriverShip.Ship;

internal class HandleClientRequest
{
	private readonly TcpClient _client;

	private readonly ShipProtocol _shipProtocol;

	private readonly HostApi _hostApi;

	private NetworkStream _stream;

	private CancellationToken _cts;

	public HandleClientRequest(ShipProtocol ship, HostApi hostApi, TcpClient client)
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		if (ship == null)
		{
			throw new ArgumentNullException("ship");
		}
		if (hostApi == null)
		{
			throw new ArgumentNullException("hostApi");
		}
		_cts = default(CancellationToken);
		_client = client;
		_shipProtocol = ship;
		_hostApi = hostApi;
	}

	public void Start()
	{
		ProceedClientConnection();
	}

	public void Stop()
	{
	}

	private void ProceedClientConnection()
	{
		Trace.Debug("HandleClientRequest.ProceedClientConnection", "ProceedClientConnection", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\HandleClientRequest.cs", 47);
		try
		{
			_stream = _client.GetStream();
			if (_stream != null)
			{
				Trace.Debug("GotStream", "ProceedClientConnection", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\HandleClientRequest.cs", 54);
				WaitForRequest();
			}
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "ProceedClientConnection", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\HandleClientRequest.cs", 59);
		}
	}

	public void WaitForRequest()
	{
		Trace.Debug("WaitForRequest enter", "WaitForRequest", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\HandleClientRequest.cs", 65);
		byte[] array = new byte[_client.ReceiveBufferSize];
		if (_stream == null)
		{
			Trace.Warning("WaitForRequest - Weird null stream", "WaitForRequest", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\HandleClientRequest.cs", 69);
		}
		else
		{
			_stream.BeginRead(array, 0, array.Length, ReadCallback, array);
		}
	}

	private void ReadCallback(IAsyncResult result)
	{
		Trace.Debug("HandleClientRequest.ReadCallback enter", "ReadCallback", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\HandleClientRequest.cs", 77);
		NetworkStream stream = _client.GetStream();
		try
		{
			int num = stream.EndRead(result);
			if (num == 0)
			{
				_stream.Close();
				_client.Close();
				Trace.Warning("HandleClientRequest.ReadCallback nothing found, read == 0", "ReadCallback", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\HandleClientRequest.cs", 86);
				return;
			}
			byte[] bytes = result.AsyncState as byte[];
			string @string = Encoding.UTF8.GetString(bytes, 0, num);
			string response = ProceedRequest(@string);
			SendResponse(response);
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "ReadCallback", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\HandleClientRequest.cs", 98);
			return;
		}
		WaitForRequest();
	}

	private void SendResponse(string response)
	{
		try
		{
			if (_stream != null && !_cts.IsCancellationRequested && !string.IsNullOrWhiteSpace(response) && !response.ToLower().Contains("error"))
			{
				Trace.Debug("Echo to client:", "SendResponse", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\HandleClientRequest.cs", 112);
				byte[] array = GenerateFrame(response);
				_stream.Write(array, 0, array.Length);
				_stream.Flush();
				Trace.Debug("Sent to client ok", "SendResponse", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\HandleClientRequest.cs", 118);
			}
			else
			{
				byte[] array2 = GenerateFrame("error");
				_stream.Write(array2, 0, array2.Length);
				_stream.Flush();
				Trace.Debug("Error response sent", "SendResponse", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\HandleClientRequest.cs", 126);
			}
		}
		catch (Exception arg)
		{
			Trace.Error($"HandleClientRequest.SendResponse  ex for {response}>>> {arg}", "SendResponse", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\HandleClientRequest.cs", 131);
		}
	}

	private string ProceedRequest(string receivedRequest)
	{
		Trace.Debug("HandleClientRequest.ProceedRequest", "ProceedRequest", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\HandleClientRequest.cs", 137);
		if (string.IsNullOrWhiteSpace(receivedRequest))
		{
			Trace.Debug("HandleClientRequest.ProceedRequest: request is null|empty", "ProceedRequest", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\HandleClientRequest.cs", 142);
			return "error Empty request";
		}
		string text = ExtractRequestNameFromRequest(receivedRequest);
		Trace.Debug("ProceedRequest client request= " + text + " / " + receivedRequest, "ProceedRequest", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\HandleClientRequest.cs", 148);
		return text switch
		{
			"GetInfo" => _hostApi.ApiInfo, 
			"GetKeyData" => _hostApi.GetKeyData(receivedRequest), 
			"GetKeyDataForNewKey" => _hostApi.GetKeyDataForNewKey(receivedRequest), 
			_ => "ProceedRequest: error unknown RequestName", 
		};
	}

	private string ExtractRequestNameFromRequest(string receivedRequest)
	{
		string text = "";
		if (string.IsNullOrWhiteSpace(text) && receivedRequest.Contains("RequestName"))
		{
			string text2 = receivedRequest.Substring(receivedRequest.IndexOf("RequestName", StringComparison.InvariantCulture) + 12);
			if (text2.Contains("<"))
			{
				text2 = text2.Substring(0, text2.IndexOf("<", StringComparison.InvariantCulture));
			}
			if (!text2.Contains("<") && !string.IsNullOrWhiteSpace(text2))
			{
				text = text2.Trim();
				Trace.Debug("HandleClientRequest.ExtractRequestNameFromRequest finally found a method: " + text, "ExtractRequestNameFromRequest", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\HandleClientRequest.cs", 184);
			}
			else
			{
				Trace.Warning("HandleClientRequest.ExtractRequestNameFromRequest method not found", "ExtractRequestNameFromRequest", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\HandleClientRequest.cs", 187);
			}
		}
		return text;
	}

	private byte[] GenerateFrame(string saltoFrame)
	{
		string s = $"STP/00/{saltoFrame.Length}/" + saltoFrame;
		return Encoding.UTF8.GetBytes(s);
	}
}
