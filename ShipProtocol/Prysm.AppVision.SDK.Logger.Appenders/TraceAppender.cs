#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Prysm.AppVision.SDK.Logger.Appenders;

public class TraceAppender : ILogAppender, IDisposable
{
	private readonly string _prefix;

	public TraceAppender(string prefix, List<TraceListener> listeners = null)
	{
		System.Diagnostics.Trace.AutoFlush = true;
		_prefix = (string.IsNullOrEmpty(prefix) ? "" : (prefix + ": "));
		if (listeners != null && listeners.Any())
		{
			foreach (TraceListener listener in listeners)
			{
				System.Diagnostics.Trace.Listeners.Add(listener);
			}
		}
		foreach (TraceListener listener2 in System.Diagnostics.Trace.Listeners)
		{
			System.Diagnostics.Trace.WriteLine("Configured Trace listener name = " + listener2.Name);
			System.Diagnostics.Trace.WriteLine($"                          type = {listener2.GetType()}");
		}
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
		}
	}

	private void WriteSync(string message, LogLevel level)
	{
		try
		{
			switch (level)
			{
			case LogLevel.Error:
				System.Diagnostics.Trace.WriteLine("*************************************************");
				System.Diagnostics.Trace.TraceError(_prefix + " - " + message);
				System.Diagnostics.Trace.WriteLine("*************************************************");
				break;
			case LogLevel.Warning:
				System.Diagnostics.Trace.TraceWarning(_prefix + " - " + message);
				break;
			case LogLevel.Info:
				System.Diagnostics.Trace.TraceInformation(_prefix + " - " + message);
				break;
			default:
				System.Diagnostics.Trace.WriteLine(_prefix + " - " + message);
				break;
			}
		}
		catch (Exception)
		{
		}
	}

	public void Dispose()
	{
		System.Diagnostics.Trace.Close();
	}
}
