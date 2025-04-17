namespace Prysm.AppVision.SDK.Logger.Appenders;

public class AppenderInfo
{
	public ILogAppender Appender { get; }

	public LogLevel Level { get; set; }

	public string AppenderName { get; }

	public AppenderInfo(ILogAppender appender, LogLevel level, string name)
	{
		Appender = appender;
		Level = level;
		AppenderName = name;
	}
}
