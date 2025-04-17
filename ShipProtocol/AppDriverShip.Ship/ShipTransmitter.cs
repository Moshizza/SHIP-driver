using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Apis.Salto.Ship.Api;
using Apis.Salto.Ship.Communication;
using Prysm.AppVision.Common;
using Prysm.AppVision.SDK;

namespace AppDriverShip.Ship;

internal class ShipTransmitter
{
	private readonly string _address;

	private readonly int _port;

	private readonly SaltoFormatter _saltoFormatter;

	private readonly ShipProtocol _shipProtocol;

	private bool _useTcpFrames = true;

	private string _username;

	private string _password;

	private string _saltoShipHttpsKey;

	private HttpClient _httpClient;

	internal ShipTransmitter(string address, int port, bool useTcp = true, string basicUsername = "", string basicPassword = "", string saltoShipHttpsKey = "")
	{
		if (string.IsNullOrWhiteSpace(address))
		{
			throw new ArgumentNullException("address");
		}
		if (port < 1)
		{
			throw new ArgumentException("port must be > 0");
		}
		_useTcpFrames = useTcp;
		_address = address;
		_port = port;
		_saltoFormatter = new SaltoFormatter();
		_shipProtocol = new ShipProtocol();
		_username = basicUsername;
		_password = basicPassword;
		_saltoShipHttpsKey = saltoShipHttpsKey;
		if (!useTcp)
		{
			InitHttps();
		}
	}

	internal Task<string> SendRequest(string method, string prm = null)
	{
		if (_useTcpFrames)
		{
			return SendTcpFrame(method, prm);
		}
		return SendHttpsFrame(method, prm);
	}

	private Uri GetHttpsUri(string url = "")
	{
		return Helper.ParseUri(_address, "https://", _port, url);
	}

	private void InitHttps()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected O, but got Unknown
		Uri httpsUri = GetHttpsUri();
		_httpClient = new HttpClient();
		if (!string.IsNullOrWhiteSpace(_username) && !string.IsNullOrWhiteSpace(_password))
		{
			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(_username + ":" + _password)));
		}
		_httpClient.BaseAddress = httpsUri;
	}

	private async Task<string> SendHttpsFrame(string method, string prm)
	{
		_ = 1;
		try
		{
			if (prm == null)
			{
				prm = "";
			}
			string text = _saltoFormatter.GenerateFrame(method, prm);
			HttpRequestMessage val = new HttpRequestMessage();
			if (!string.IsNullOrWhiteSpace(_saltoShipHttpsKey))
			{
				((HttpHeaders)val.Headers).Add("Salto-SHIP-Key", _saltoShipHttpsKey);
			}
			StringContent val2 = new StringContent(text);
			((HttpContent)val2).Headers.ContentType = new MediaTypeHeaderValue("application/vnd.saltosystems.ship.v1+xml");
			val.Content = (HttpContent)(object)val2;
			val.Method = HttpMethod.Post;
			HttpResponseMessage val3 = await _httpClient.SendAsync(val);
			_ = val3.StatusCode;
			switch (val3.StatusCode)
			{
			case HttpStatusCode.Unauthorized:
				Trace.Error("Send https frame unauthorized. Check your config", "SendHttpsFrame", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipTransmitter.cs", 100);
				throw new Exception("Https unauthorized. Please check your config");
			case HttpStatusCode.NotImplemented:
				Trace.Error("Not implemented. Please activate the HttpTransport in the SaltoDB", "SendHttpsFrame", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipTransmitter.cs", 103);
				throw new Exception("Not implemented. Please activate the HttpTransport in the SaltoDB");
			case HttpStatusCode.UnsupportedMediaType:
				Trace.Error("Unsupported media type. Please contact your retailer. Check your SHIP/Space version", "SendHttpsFrame", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipTransmitter.cs", 106);
				throw new Exception("Unsupported media type. Please contact your retailer. Check your SHIP/Space version");
			default:
				Trace.Error($"Result code not expected: {val3.StatusCode}", "SendHttpsFrame", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipTransmitter.cs", 111);
				throw new Exception($"Result code not expected: {val3.StatusCode}");
			case HttpStatusCode.OK:
				return await val3.Content.ReadAsStringAsync();
			}
		}
		catch (Exception ex)
		{
			Trace.Error($"SHIP - Can not send Http frame: {ex.Message} with > {method} and {prm} > {ex}", "SendHttpsFrame", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipTransmitter.cs", 119);
			throw;
		}
	}

	private async Task<string> SendTcpFrame(string method, string prm)
	{
		try
		{
			string frame = _saltoFormatter.GenerateFrame(method, prm);
			using TcpClient client = new TcpClient(_address, _port);
			using NetworkStream networkStream = client.GetStream();
			client.ReceiveTimeout = 5000;
			client.SendTimeout = 3000;
			if (!networkStream.CanWrite)
			{
				throw new Exception($"ShipTransmitter - Can not write networkStream to {_address}:{_port}");
			}
			await _shipProtocol.SendFrame(networkStream, frame);
			if (!networkStream.CanRead)
			{
				throw new Exception($"ShipTransmitter - Can not read networkStream to {_address}:{_port}");
			}
			return _shipProtocol.ReceiveShipResponse(networkStream, method);
		}
		catch (Exception ex)
		{
			Trace.Error($"can not send tcp frame with {method} and {prm} > {ex.Message} > {ex}", "SendTcpFrame", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipTransmitter.cs", 164);
			return "";
		}
	}
}
