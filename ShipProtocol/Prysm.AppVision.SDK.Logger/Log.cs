#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Prysm.AppVision.SDK.Logger.Appenders;
using Prysm.AppVision.SDK.Logger.Tools;

namespace Prysm.AppVision.SDK.Logger;

public class Log : MarshalByRefObject, ILog, IAppenderManager, IDisposable
{
	private readonly List<AppenderInfo> _appenders = new List<AppenderInfo>();

	private PerfCounters _counters = new PerfCounters();

	public List<AppenderInfo> Appenders => _appenders;

	public event Action<LogEntry> LogEvent;

	public void AddAppender(ILogAppender appender, LogLevel level, string name)
	{
		if (level != LogLevel.None)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				name = "default appender";
			}
			if (!_appenders.Any((AppenderInfo x) => x.AppenderName == name))
			{
				_appenders.Add(new AppenderInfo(appender, level, name));
				Message(LogLevel.Info, $"Added log output: {name} > {appender.GetType()}", "", "", 0);
			}
		}
	}

	public void RemoveAppender(string name)
	{
		AppenderInfo appenderInfo = _appenders.FirstOrDefault((AppenderInfo x) => x.AppenderName == name);
		if (appenderInfo != null)
		{
			_appenders.Remove(appenderInfo);
			Message(LogLevel.Info, "Removed log output: " + name, "", "", 0);
		}
	}

	[DebuggerStepThrough]
	public void PerfCounterStart(string counterName, string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Message(LogLevel.Debug, _counters.Start(counterName, message), memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public void PerfCounterStop(string counterName, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Message(LogLevel.Debug, _counters.Stop(counterName), memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Message(LogLevel.Debug, message, memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public void Debug(Exception ex, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Message(LogLevel.Debug, ex?.ToString(), memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Message(LogLevel.Info, message, memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public void Warn(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Message(LogLevel.Warning, message, memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public void Warn(Exception ex, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Message(LogLevel.Warning, ex?.ToString(), memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public void Warn(string message, Exception ex, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Message(LogLevel.Warning, $"{message}: {ex}", memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public void Error(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Message(LogLevel.Error, message, memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public void Error(string message, Exception ex, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Message(LogLevel.Error, $"{message}: {ex}", memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public void Error(Exception ex, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		Message(LogLevel.Error, ex?.ToString(), memberName, sourceFilePath, sourceLineNumber);
	}

	[DebuggerStepThrough]
	public void Dispose()
	{
		foreach (AppenderInfo appender in _appenders)
		{
			appender?.Appender?.Dispose();
		}
	}

	[DebuggerStepThrough]
	internal string Message(LogLevel level, string message, string memberName, string sourceFilePath, int sourceLineNumber)
	{
		string text = $"{memberName} [{sourceFilePath} at l.{sourceLineNumber}]";
		if (string.IsNullOrWhiteSpace(memberName))
		{
			text = FindCallingMethod();
		}
		string text2 = $"{Process.GetCurrentProcess().Id}.{Thread.CurrentThread.ManagedThreadId}";
		string formattedMessage = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}   {level.ToString().ToUpper()}    ({text2}) {text}   - {message}";
		foreach (AppenderInfo appender in _appenders)
		{
			if (appender.Level <= level)
			{
				try
				{
					appender.Appender.Write(formattedMessage, level);
				}
				catch (Exception arg)
				{
					System.Diagnostics.Trace.TraceError($"Appender write exception [{appender?.AppenderName}] > {arg}");
				}
			}
		}
		if (!_appenders.Any())
		{
			System.Diagnostics.Trace.TraceError("Logger does not contains any appender. Check config > Msg=" + message);
		}
		try
		{
			Task.Run(delegate
			{
				try
				{
					this.LogEvent?.Invoke(new LogEntry(level, formattedMessage));
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
		return text + "   - " + message;
	}

	private string FindCallingMethod()
	{
		try
		{
			string assemblyLocation = GetAssemblyLocation();
			StackTrace stackTrace = new StackTrace();
			for (int i = 1; i < stackTrace.FrameCount; i++)
			{
				MethodBase method = stackTrace.GetFrame(i).GetMethod();
				string fullyQualifiedName = method.Module.FullyQualifiedName;
				if (!string.IsNullOrWhiteSpace(assemblyLocation) && fullyQualifiedName == assemblyLocation)
				{
					continue;
				}
				LoggerAttribute[] obj = method.GetCustomAttributes(typeof(LoggerAttribute), inherit: true) as LoggerAttribute[];
				if (obj == null || !obj.Any((LoggerAttribute x) => x?.IgnoreInTree ?? false))
				{
					string text = method?.ReflectedType?.FullName ?? "";
					if (string.IsNullOrWhiteSpace(text))
					{
						System.Diagnostics.Trace.TraceInformation("Can not find reflected method name");
						System.Diagnostics.Trace.TraceInformation("reflected subsys=" + method?.ReflectedType?.UnderlyingSystemType?.FullName);
						System.Diagnostics.Trace.TraceInformation("declaring namespace=" + method?.DeclaringType?.Namespace);
						System.Diagnostics.Trace.TraceInformation("declaring name=" + method?.DeclaringType?.Name);
						System.Diagnostics.Trace.TraceInformation("declaring subsystem=" + method?.DeclaringType?.UnderlyingSystemType?.FullName);
						System.Diagnostics.Trace.TraceInformation("declaring fullname=" + method?.DeclaringType?.FullName);
						System.Diagnostics.Trace.TraceInformation("module Name=" + method?.Module?.Name);
						System.Diagnostics.Trace.TraceInformation("module scope=" + method?.Module?.ScopeName);
						System.Diagnostics.Trace.TraceInformation($"module to string={method?.Module}");
						System.Diagnostics.Trace.TraceInformation($"callingconv={method?.CallingConvention}");
						System.Diagnostics.Trace.TraceInformation($"membertype={method?.MemberType}");
						System.Diagnostics.Trace.TraceInformation("method name=" + method?.Name);
						System.Diagnostics.Trace.TraceInformation($"gettype={method?.GetType()}");
						text = method?.Module?.Name;
					}
					return (text + "." + method?.Name).Replace('\u202e', ' ');
				}
			}
		}
		catch (Exception arg)
		{
			System.Diagnostics.Trace.TraceError($"Can not find calling method > {arg}");
		}
		return "";
	}

	private string GetAssemblyLocation()
	{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		try
		{
			return executingAssembly?.Modules?.FirstOrDefault()?.FullyQualifiedName;
		}
		catch (Exception arg)
		{
			System.Diagnostics.Trace.TraceError($"GetAssembly location ex > {arg}");
			return executingAssembly?.Modules?.FirstOrDefault()?.FullyQualifiedName;
		}
	}
}
