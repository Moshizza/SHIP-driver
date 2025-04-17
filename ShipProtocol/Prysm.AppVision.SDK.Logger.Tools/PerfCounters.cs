using System;
using System.Collections.Concurrent;

namespace Prysm.AppVision.SDK.Logger.Tools;

internal class PerfCounters
{
	private ConcurrentDictionary<string, PerfCounter> _counters = new ConcurrentDictionary<string, PerfCounter>();

	public string Start(string name, string message = "")
	{
		if (_counters.ContainsKey(name))
		{
			_counters[name]?.Watcher?.Stop();
			string result = "[PERFORMANCE] <" + name + "> RESTART after > " + _counters[name]?.GetElapsedTime();
			_counters[name].Watcher.Restart();
			return result;
		}
		PerfCounter value = new PerfCounter(name, message);
		if (!_counters.TryAdd(name, value))
		{
			return "START " + name + " (counter not started)";
		}
		return "[PERFORMANCE] <" + name + "> START >> " + message;
	}

	public string Stop(string name)
	{
		if (!_counters.ContainsKey(name))
		{
			return ">>>>>> STOP " + name + " (counter not found) <<<<<<";
		}
		string result = "";
		try
		{
			_counters[name]?.Watcher?.Stop();
			result = "[PERFORMANCE] <" + name + "> STOP >> " + _counters[name]?.GetElapsedTime();
			_counters[name] = null;
			_counters.TryRemove(name, out var _);
		}
		catch (Exception ex)
		{
			Trace.Debug(ex, null, "Stop", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Log\\Tools\\PerfCounters.cs", 45);
		}
		return result;
	}
}
