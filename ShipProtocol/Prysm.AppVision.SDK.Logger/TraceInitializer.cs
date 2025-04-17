using System;
using System.Collections.Generic;
using System.Diagnostics;
using Prysm.AppVision.SDK.Logger.Appenders;

namespace Prysm.AppVision.SDK.Logger;

internal class TraceInitializer
{
	private IAppenderManager _log;

	public TraceInitializer(IAppenderManager log)
	{
		if (log == null)
		{
			throw new ArgumentNullException("ILog can not be null");
		}
		_log = log;
		if (!Debugger.IsAttached)
		{
			Trace("", LogLevel.All, null, "MainAppenderForAll");
		}
		else
		{
			Console(LogLevel.All, "MainAppenderForAll");
		}
	}

	public void SetGeneralLogLevel(LogLevel level)
	{
		foreach (AppenderInfo appender in _log.Appenders)
		{
			if (!(appender.AppenderName == "MainAppenderForAll"))
			{
				appender.Level = level;
			}
		}
	}

	public void Console(LogLevel level, string appenderName = "traceAppender")
	{
		_log.AddAppender(new ConsoleAppender(), level, appenderName);
	}

	public void Trace(string prefix, LogLevel level, TraceListener listener = null, string appenderName = "traceAppender")
	{
		List<TraceListener> list = new List<TraceListener>();
		if (listener != null)
		{
			list.Add(listener);
		}
		_log.AddAppender(new TraceAppender(prefix, list), level, appenderName);
	}

	public void TempFile(string filename, LogLevel level)
	{
		_log.AddAppender(new TempFileAppender(filename), level, "temp file appender");
	}

	public void File(string fullfilename, LogLevel level)
	{
		_log.AddAppender(new BinFileAppender(fullfilename), level, "file appender");
	}

	public void CallingFile(string prefix, LogLevel level)
	{
		_log.AddAppender(new CallingFileAppender(prefix), level, "calling appender");
	}

	public void WindowsLog(string fullfilename, LogLevel level)
	{
		_log.AddAppender(new WindowsLogAppender(fullfilename), level, "windows appender");
	}
}
