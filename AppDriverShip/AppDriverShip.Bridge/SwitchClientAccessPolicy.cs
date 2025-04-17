using System.Net;

namespace AppDriverShip.Bridge;

internal class SwitchClientAccessPolicy : ISwitchStrategy
{
	public string DoConditionnal(HttpListenerRequest request)
	{
		return GetText();
	}

	public string DoConditionnal(string parameters, HttpListenerRequest request)
	{
		return GetText();
	}

	private string GetText()
	{
		return "<?xml version=\"1.0\" encoding=\"utf-8\"?><access-policy><cross-domain-access><policy><allow-from http-request-headers=\"*\"><domain uri=\"*\"/><domain uri=\"http://*\"/></allow-from><grant-to><resource path=\"/\" include-subpaths=\"true\"/></grant-to></policy></cross-domain-access></access-policy>";
	}
}
