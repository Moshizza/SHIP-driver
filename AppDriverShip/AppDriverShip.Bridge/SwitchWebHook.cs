using System;
using System.IO;
using System.Net;
using AppDriverShip.Model;
using AppDriverShip.Salto;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Prysm.AppVision.SDK;

namespace AppDriverShip.Bridge;

internal class SwitchWebHook : ISwitchStrategy
{
	internal event Action<HookEvent> EventReceived;

	public string DoConditionnal(HttpListenerRequest request)
	{
		Run(request);
		return "";
	}

	public string DoConditionnal(string parameters, HttpListenerRequest request)
	{
		Run(request);
		return "";
	}

	private void Run(HttpListenerRequest request)
	{
		string body = ReadBody(request);
		ParseJson(body);
	}

	private string ReadBody(HttpListenerRequest request)
	{
		Trace.Debug("WebHook readBody", "ReadBody", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchWebHook.cs", 40);
		if (!request.HasEntityBody)
		{
			return "";
		}
		string text = "";
		using StreamReader streamReader = new StreamReader(request.InputStream, request.ContentEncoding);
		if (request.ContentType != null)
		{
			Trace.Debug("content type " + request.ContentType, "ReadBody", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchWebHook.cs", 48);
		}
		Trace.Debug($"content length {request.ContentLength64}", "ReadBody", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchWebHook.cs", 49);
		return streamReader.ReadToEnd();
	}

	public void ParseJson(string body)
	{
		Trace.Debug("WebHook got body: " + body, "ParseJson", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchWebHook.cs", 58);
		try
		{
			HookEvent hookEvent = JsonConvert.DeserializeObject<HookEvent>(body, new JsonConverter[1]
			{
				new IsoDateTimeConverter
				{
					DateTimeFormat = "yyyy-MM-dd HH:mm:ss"
				}
			});
			Trace.Debug("Deserialized hook > doorname:" + hookEvent?.DoorName, "ParseJson", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchWebHook.cs", 63);
			if (hookEvent != null)
			{
				EventHelper.InvokeAsync(this.EventReceived, hookEvent);
			}
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "ParseJson", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Bridge\\SwitchWebHook.cs", 71);
		}
	}
}
