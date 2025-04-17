using System.Net;

namespace AppDriverShip.Bridge;

internal class SwitchTest : ISwitchStrategy
{
	public string DoConditionnal(HttpListenerRequest request)
	{
		return "SwitchTest OK - Can retreive salto binaries";
	}

	public string DoConditionnal(string parameters, HttpListenerRequest request)
	{
		return "Switch test ok with param " + parameters + " - Can retreive salto binaries";
	}
}
