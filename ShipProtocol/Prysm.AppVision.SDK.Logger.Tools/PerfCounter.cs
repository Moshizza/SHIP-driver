using System.Diagnostics;

namespace Prysm.AppVision.SDK.Logger.Tools;

internal class PerfCounter
{
	public string Name { get; set; }

	public string Message { get; set; }

	public Stopwatch Watcher { get; set; }

	public PerfCounter(string name, string messagge = "")
	{
		Name = name;
		Message = messagge;
		Watcher = new Stopwatch();
		Watcher.Start();
	}

	public string GetElapsedTime()
	{
		if (Watcher.ElapsedMilliseconds < 1000)
		{
			return $"{Watcher.ElapsedMilliseconds}'{Watcher.ElapsedTicks} ms";
		}
		return $"{Watcher.Elapsed.Hours:00}:{Watcher.Elapsed.Minutes:00}:{Watcher.Elapsed.Seconds:00} {Watcher.Elapsed.Milliseconds}'{Watcher.Elapsed.Ticks}";
	}
}
