using System;
using System.Net;
using System.Threading;

namespace AppDriverShip.Bridge;

public class HttpListenerCallbackState
{
	private readonly HttpListener _listener;

	private readonly AutoResetEvent _listenForNextRequest;

	public HttpListener Listener => _listener;

	public AutoResetEvent ListenForNextRequest => _listenForNextRequest;

	public HttpListenerCallbackState(HttpListener listener)
	{
		if (listener == null)
		{
			throw new ArgumentNullException("listener");
		}
		_listener = listener;
		_listenForNextRequest = new AutoResetEvent(initialState: false);
	}
}
