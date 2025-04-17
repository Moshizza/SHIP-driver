using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AppDriverShip.Bridge;

internal static class ResponseContext
{
	private static readonly Dictionary<string, ISwitchStrategy> _strategies;

	public static Dictionary<string, ISwitchStrategy> Strategies => _strategies;

	static ResponseContext()
	{
		_strategies = new Dictionary<string, ISwitchStrategy>();
		_strategies.Add("/clientaccesspolicy.xml", new SwitchClientAccessPolicy());
		_strategies.Add("/crossdomain.xml", new SwitchCrossDomain());
	}

	public static bool AddSwitchStrategy(string key, ISwitchStrategy strategy)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return false;
		}
		if (_strategies.ContainsKey(key))
		{
			return false;
		}
		if (_strategies.ContainsKey(key.ToLower()))
		{
			return false;
		}
		if (strategy == null)
		{
			return false;
		}
		_strategies.Add(key, strategy);
		return true;
	}

	public static bool RemoveStrategy(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return false;
		}
		if (!_strategies.ContainsKey(key) && !_strategies.ContainsKey(key.ToLower()))
		{
			return false;
		}
		if (_strategies.ContainsKey(key))
		{
			_strategies.Remove(key);
		}
		if (_strategies.ContainsKey(key.ToLower()))
		{
			_strategies.Remove(key.ToLower());
		}
		return true;
	}

	public static string GetResponseForUrl(string conditionnal, HttpListenerRequest request)
	{
		if (_strategies.ContainsKey(conditionnal))
		{
			return _strategies[conditionnal].DoConditionnal(request);
		}
		string text = "";
		string parameters = "";
		if (conditionnal.Contains("/"))
		{
			string[] array = conditionnal.Split('/');
			if (array.Length > 1)
			{
				int num = ((!(array[1].ToLower() == "drvsalto")) ? 1 : 2);
				text = array[num];
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = num + 1; i < array.Length; i++)
				{
					stringBuilder.Append(array[i] + "/");
				}
				parameters = stringBuilder.ToString();
			}
		}
		foreach (string key in _strategies.Keys)
		{
			if (key.ToLower() == conditionnal.ToLower())
			{
				return _strategies[key].DoConditionnal(parameters, request);
			}
			if (!string.IsNullOrWhiteSpace(text) && "/" + text.ToLower() == conditionnal.ToLower())
			{
				return _strategies[key].DoConditionnal(parameters, request);
			}
		}
		foreach (string key2 in _strategies.Keys)
		{
			if (key2.ToLower().StartsWith(conditionnal.ToLower(), StringComparison.InvariantCulture))
			{
				return _strategies[key2].DoConditionnal(parameters, request);
			}
		}
		foreach (string key3 in _strategies.Keys)
		{
			if (key3.ToLower().Contains(conditionnal.ToLower()))
			{
				return _strategies[key3].DoConditionnal(parameters, request);
			}
			if (!string.IsNullOrWhiteSpace(text) && key3.ToLower().Contains(text.ToLower()))
			{
				return _strategies[key3].DoConditionnal(parameters, request);
			}
		}
		return "";
	}
}
