using System.Net;

namespace AppDriverShip.Bridge;

internal interface ISwitchStrategy
{
	string DoConditionnal(HttpListenerRequest request);

	string DoConditionnal(string parameters, HttpListenerRequest request);
}
