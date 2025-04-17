using System;

namespace Prysm.AppVision.SDK;

public class LogEntry
{
	public DateTime Date { get; set; }

	public LogLevel Level { get; set; }

	public string Message { get; set; }

	public LogEntry(LogLevel level, string message)
	{
		Date = DateTime.Now;
		Level = level;
		Message = message;
	}

	public override string ToString()
	{
		return $"{Date:yyyy-MM-dd HH:mm:ss.f}\t{Level}\t> {Message}";
	}
}
