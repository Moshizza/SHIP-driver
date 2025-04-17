using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Prysm.AppVision.SDK.Logger.Appenders;

public class WindowsLogAppender : ILogAppender, IDisposable
{
	private bool _failed;

	public WindowsLogAppender(string fullfilename)
	{
		if (string.IsNullOrWhiteSpace(fullfilename))
		{
			fullfilename = Assembly.GetExecutingAssembly().Location;
		}
		fullfilename = Path.GetFileName(fullfilename);
	}

	public async Task Write(string message, LogLevel level)
	{
		try
		{
			await Task.Run(delegate
			{
				WriteSync(message, level);
			});
		}
		catch (Exception)
		{
			_failed = true;
		}
	}

	private void WriteSync(string message, LogLevel level)
	{
		try
		{
			if (_failed)
			{
				return;
			}
			using EventLog eventLog = new EventLog("Application");
			eventLog.Source = "Application";
			switch (level)
			{
			case LogLevel.Error:
				eventLog.WriteEntry(message, EventLogEntryType.Error);
				break;
			case LogLevel.Warning:
				eventLog.WriteEntry(message, EventLogEntryType.Warning);
				break;
			default:
				eventLog.WriteEntry(message, EventLogEntryType.Information);
				break;
			}
		}
		catch (Exception)
		{
			_failed = true;
		}
	}

	public void Dispose()
	{
	}
}
