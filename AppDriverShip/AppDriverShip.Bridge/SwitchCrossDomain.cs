using System.Net;

namespace AppDriverShip.Bridge;

internal class SwitchCrossDomain : ISwitchStrategy
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
		return "<?xml version=\"1.0\"?><!DOCTYPE cross-domain-policy SYSTEM \"http://www.macromedia.com/xml/dtds/cross-domain-policy.dtd\"><cross-domain-policy><allow-http-request-headers-from domain=\"*\" headers=\"*\" /><allow-access-from domain=\"*\" /><!--<allow-http-request-headers-from domain=\"*\" headers=\"SOAPAction\" />--></cross-domain-policy>";
	}
}
