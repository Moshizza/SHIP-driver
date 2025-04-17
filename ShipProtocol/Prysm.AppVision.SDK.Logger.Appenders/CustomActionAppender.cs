using System;
using System.Threading.Tasks;

namespace Prysm.AppVision.SDK.Logger.Appenders;

public class CustomActionAppender : ILogAppender, IDisposable
{
	private readonly Action<string, LogLevel> _logAction;

	public CustomActionAppender(Action<string, LogLevel> logAction)
	{
		_logAction = logAction;
	}

	public void Dispose()
	{
	}

	public Task Write(string message, LogLevel level)
	{
		_logAction(message, level);
		return Task.FromResult(0);
	}
}
