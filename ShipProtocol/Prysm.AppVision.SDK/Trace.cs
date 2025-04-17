#define TRACE
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Prysm.AppVision.SDK.Logger;

namespace Prysm.AppVision.SDK;

public static class Trace
{
	private static readonly Log _logger;

	private static TraceInitializer _traceInitializer;

	private static LogLevel _generalLogLevel;

	private static bool _initialized;

	public static Log Logger { get; }

	public static LogLevel CurrentLogLevel
	{
		get
		{
			return _generalLogLevel;
		}
		set
		{
			_generalLogLevel = value;
			if (_traceInitializer == null)
			{
				_traceInitializer = new TraceInitializer(Logger);
			}
			_traceInitializer.SetGeneralLogLevel(value);
		}
	}

	public static string StatusText { get; set; }

	[Obsolete("This property is obsolete. Logger.LogEvent.", false)]
	public static event Action<LogEntry> LogEvent;

	static Trace()
	{
		_logger = new Log();
		Logger = _logger;
		_generalLogLevel = LogLevel.Warning;
		_initialized = false;
		_traceInitializer = new TraceInitializer(Logger);
		Logger.Message(LogLevel.Info, $"Operating system: {Environment.OSVersion}", "", "", 0);
		Logger.Message(LogLevel.Info, "Computer name: " + Environment.MachineName, "", "", 0);
		Logger.Message(LogLevel.Info, "User name: " + Environment.UserName, "", "", 0);
		Logger.Message(LogLevel.Info, $"CLR runtime version: {Environment.Version}", "", "", 0);
		Logger.Message(LogLevel.Info, "Command line: " + Environment.CommandLine, "", "", 0);
	}

	[DebuggerStepThrough]
	public static void StartCounter(string name, string message = "", [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Logger.PerfCounterStart(name, message, memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public static void StopCounter(string name, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Logger.PerfCounterStop(name, memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public static void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Log(LogLevel.Debug, message, memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public static void DebugWriteLine(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Log(LogLevel.All, message, memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public static void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Log(LogLevel.Info, message, memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public static void Warning(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Log(LogLevel.Warning, message, memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public static void Error(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Log(LogLevel.Error, message, memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public static void Debug(Exception ex, string message = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Debug(BuildMessageFromEx(ref ex, message), memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public static void Error(Exception ex, string message = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Error(BuildMessageFromEx(ref ex, message), memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	private static string BuildMessageFromEx(ref Exception ex, string message)
	{
		if (ex == null && string.IsNullOrWhiteSpace(message))
		{
			return "";
		}
		string text = "";
		if (!string.IsNullOrWhiteSpace(message))
		{
			text = text + message + ": ";
		}
		return text + ex;
	}

	private static void Log(LogLevel level, string message, string memberName, string sourceFilePath, int sourceLineNumber)
	{
		if (_traceInitializer == null)
		{
			_traceInitializer = new TraceInitializer(Logger);
		}
		if (string.IsNullOrWhiteSpace(message))
		{
			return;
		}
		string message2 = Logger.Message(level, message, memberName, sourceFilePath, sourceLineNumber);
		LogEntry it = new LogEntry(level, message2);
		try
		{
			Task.Run(delegate
			{
				try
				{
					Trace.LogEvent?.Invoke(it);
				}
				catch (Exception ex2)
				{
					System.Diagnostics.Trace.WriteLine(ex2?.Message ?? "");
				}
			});
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.WriteLine(ex?.Message ?? "");
		}
	}
}
