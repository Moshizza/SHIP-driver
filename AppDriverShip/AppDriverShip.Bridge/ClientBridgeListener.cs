using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Apis.Salto.Ship;
using AppDriverShip.Model;
using AppDriverShip.Salto;
using Prysm.AppVision.SDK;

namespace AppDriverShip.Bridge;

internal class ClientBridgeListener
{
	private readonly ShipApi _ship;

	private readonly AppServerForDriver _appServer;

	private static ClientBridgeListener instance;

	private static object _locker = new object();

	private int requestCounter;

	private ManualResetEvent stopEvent = new ManualResetEvent(initialState: false);

	private readonly List<string> _prefixes = new List<string>();

	private static readonly HttpListener _listener = new HttpListener();

	internal event Action<HookEvent> EventReceived;

	private ClientBridgeListener(string listenerUrl, ShipApi ship, Driver driver, string addressIn)
	{
		Trace.Debug("ClientBridgeListener ctor " + listenerUrl, ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 40);
		if (ship == null)
		{
			throw new ArgumentNullException("ship");
		}
		if (string.IsNullOrWhiteSpace(listenerUrl))
		{
			throw new ArgumentNullException(listenerUrl);
		}
		if (driver == null)
		{
			throw new ArgumentNullException("driver");
		}
		_ship = ship;
		if (!_prefixes.Contains(listenerUrl))
		{
			_prefixes.Add(listenerUrl);
		}
		_appServer = driver?.AppServer;
		ResponseContext.AddSwitchStrategy("/Test", new SwitchTest());
		ResponseContext.AddSwitchStrategy("/GetSaltoBinaries", new SwitchGetSaltoBinaries(ship, _appServer, addressIn));
		ResponseContext.AddSwitchStrategy("/SaltoEncoder", new SwitchDirectSaltoEncoder(ship, driver, addressIn));
		Trace.Info("Listening salto hooks on: " + listenerUrl + "/WebHook", ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 65);
		SwitchWebHook switchWebHook = new SwitchWebHook();
		ResponseContext.AddSwitchStrategy("/WebHook", switchWebHook);
		switchWebHook.EventReceived += WebHook_EventReceived;
	}

	private void WebHook_EventReceived(HookEvent obj)
	{
		EventHelper.InvokeAsync(this.EventReceived, obj);
	}

	public static ClientBridgeListener GetInstance(string listenerUrl, ShipApi ship, Driver driver, string addressIn)
	{
		Task<ClientBridgeListener> task = Task.Run(delegate
		{
			lock (_locker)
			{
				return instance ?? (instance = new ClientBridgeListener(listenerUrl, ship, driver, addressIn));
			}
		});
		if (task.Wait(TimeSpan.FromSeconds(10.0)))
		{
			Trace.Debug("Bridge found", "GetInstance", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 86);
			return task.Result;
		}
		Trace.Warning("FAILURE - Bridge TIMED OUT", "GetInstance", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 91);
		throw new Exception("Timed out");
	}

	public void Start()
	{
		Trace.Info(string.Format("Client bridge listener start with {0} prefixes > {1}", _prefixes?.Count(), string.Join(", ", _prefixes)), "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 103);
		try
		{
			foreach (string prefix in _prefixes)
			{
				try
				{
					string url = EnsureUrlValidity(prefix);
					if (!_listener.Prefixes.Any((string x) => x == url))
					{
						Trace.Debug("Try add prefix: " + prefix, "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 113);
						_listener.Prefixes.Add(url);
					}
					Trace.Debug("url ok " + url + " in prefixes", "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 116);
				}
				catch (Exception ex)
				{
					Trace.Error(prefix + " - " + ex.Message, "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 120);
				}
			}
			_listener.Start();
			Trace.Debug("Bridge listener started", "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 128);
			HttpListenerCallbackState state = new HttpListenerCallbackState(_listener);
			ThreadPool.QueueUserWorkItem(Listen, state);
			Trace.Info("client listener ready", "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 131);
		}
		catch (Exception ex2)
		{
			Trace.Error("WARNING Salto drv _listener Start exception... " + ex2?.Message, "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 135);
			Trace.Debug($"Start exception detail: {ex2}", "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 136);
			Trace.Error(" >> CONTACT YOUR IT TO CHECK ACCESS TO " + string.Join(",", _prefixes) + " with 'netsh http show urlacl'. You mais need to use 'netsh http delete urlacl url=xxxxx'", "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 137);
			Trace.Debug($"listener count: {_listener?.Prefixes?.Count()}", "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 139);
			foreach (string prefix2 in _prefixes)
			{
				Trace.Debug("prefix: " + prefix2, "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 141);
			}
			throw;
		}
	}

	private static string EnsureUrlValidity(string listenerUrl)
	{
		string text = listenerUrl;
		if (!text.ToLower().StartsWith("http", StringComparison.InvariantCulture))
		{
			text = "https://" + text;
		}
		if (!text.ToLower().EndsWith("/", StringComparison.InvariantCulture))
		{
			text += "/";
		}
		return text;
	}

	public void Stop()
	{
		try
		{
			stopEvent.Set();
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "Stop", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 168);
		}
	}

	private void Listen(object state)
	{
		HttpListenerCallbackState httpListenerCallbackState = (HttpListenerCallbackState)state;
		while (httpListenerCallbackState.Listener.IsListening)
		{
			httpListenerCallbackState.Listener.BeginGetContext(ListenerCallback, httpListenerCallbackState);
			if (WaitHandle.WaitAny(new WaitHandle[2] { httpListenerCallbackState.ListenForNextRequest, stopEvent }) == 1)
			{
				httpListenerCallbackState.Listener.Stop();
				break;
			}
		}
	}

	private void ListenerCallback(IAsyncResult ar)
	{
		HttpListenerCallbackState httpListenerCallbackState = (HttpListenerCallbackState)ar.AsyncState;
		HttpListenerContext httpListenerContext = null;
		Interlocked.Increment(ref requestCounter);
		try
		{
			httpListenerContext = httpListenerCallbackState.Listener.EndGetContext(ar);
		}
		catch
		{
			return;
		}
		finally
		{
			httpListenerCallbackState.ListenForNextRequest.Set();
		}
		if (httpListenerContext == null)
		{
			return;
		}
		string rowUrl = GetRowUrl(httpListenerContext.Request);
		try
		{
			using HttpListenerResponse httpListenerResponse = httpListenerContext.Response;
			byte[] bytes = Encoding.UTF8.GetBytes(rowUrl);
			httpListenerResponse.ContentLength64 = bytes.LongLength;
			httpListenerResponse.StatusCode = 200;
			httpListenerResponse.Headers.Add(HttpResponseHeader.CacheControl, "no-cache");
			httpListenerResponse.OutputStream.Write(bytes, 0, bytes.Length);
			httpListenerResponse.Close();
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "ListenerCallback", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 231);
		}
	}

	private static string GetRowUrl(HttpListenerRequest request)
	{
		string result = "";
		try
		{
			if (!string.IsNullOrWhiteSpace(request?.RawUrl))
			{
				Trace.Debug(request?.RawUrl, "GetRowUrl", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 243);
				result = ResponseContext.GetResponseForUrl(request.RawUrl, request) ?? "";
			}
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "GetRowUrl", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\ClientBridgeListener.cs", 249);
		}
		return result;
	}
}
