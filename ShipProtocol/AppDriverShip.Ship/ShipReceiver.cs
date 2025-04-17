using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Apis.Messaging;
using Apis.Salto.Ship.Communication;
using Apis.Salto.Ship.Host;
using Prysm.AppVision.SDK;

namespace AppDriverShip.Ship;

public class ShipReceiver : IChildMessager<string>
{
	private readonly TcpListener _listener;

	private readonly IPAddress _ip;

	private int _port;

	private CancellationToken _cts;

	private readonly HostApi _hostApi;

	private readonly ShipProtocol _shipProtocol;

	private IParentMessager<string> _parentMessager;

	public ShipReceiver(int portListen, IParentMessager<string> parent, string serverIp = null)
	{
		if (portListen < 1)
		{
			throw new ArgumentException("portListen must be > 0");
		}
		try
		{
			_ip = ((serverIp == null) ? IPAddress.Any : IPAddress.Parse(serverIp));
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipReceiver.cs", 41);
			_ip = IPAddress.Any;
		}
		_port = portListen;
		_parentMessager = parent;
		_listener = new TcpListener(_ip, _port);
		_cts = default(CancellationToken);
		_hostApi = new HostApi(_parentMessager);
		_shipProtocol = new ShipProtocol();
	}

	public void RegisterParentClass(IParentMessager<string> parent)
	{
		if (parent == null)
		{
			throw new ArgumentNullException("parent");
		}
		_parentMessager = parent;
	}

	public void Start()
	{
		StartTcpListener();
	}

	public void Stop()
	{
		try
		{
			Trace.Info("Stop ShipReceiver", "Stop", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipReceiver.cs", 73);
			_cts = new CancellationToken(canceled: true);
			_listener.Stop();
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "Stop", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipReceiver.cs", 79);
		}
	}

	private void StartTcpListener()
	{
		try
		{
			if (_listener == null)
			{
				Trace.Error("listener can not be null", "StartTcpListener", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipReceiver.cs", 93);
				return;
			}
			if (_cts.IsCancellationRequested)
			{
				return;
			}
			Trace.Info("Start ShipReceiver", "StartTcpListener", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipReceiver.cs", 99);
			_listener.Start();
		}
		catch (Exception arg)
		{
			Trace.Error($"StartTcpListener Exception > {arg}", "StartTcpListener", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipReceiver.cs", 104);
			throw;
		}
		WaitForClientConnect();
	}

	private void WaitForClientConnect()
	{
		try
		{
			object state = new object();
			_listener.BeginAcceptTcpClient(OnClientConnect, state);
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "WaitForClientConnect", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipReceiver.cs", 119);
		}
	}

	private void OnClientConnect(IAsyncResult result)
	{
		WaitForClientConnect();
		HandleClientConnection(result);
	}

	private void HandleClientConnection(IAsyncResult result)
	{
		Task.Run(delegate
		{
			try
			{
				Trace.Debug($"Received connection on {_port}", "HandleClientConnection", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipReceiver.cs", 135);
				TcpClient client = _listener.EndAcceptTcpClient(result);
				new HandleClientRequest(_shipProtocol, _hostApi, client).Start();
			}
			catch (Exception ex)
			{
				Trace.Error(ex, null, "HandleClientConnection", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipReceiver.cs", 142);
			}
		});
	}
}
