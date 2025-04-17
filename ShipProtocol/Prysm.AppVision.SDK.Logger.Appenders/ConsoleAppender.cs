#define TRACE
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Prysm.AppVision.SDK.Logger.Appenders;

public class ConsoleAppender : ILogAppender, IDisposable
{
	public async Task Write(string message, LogLevel level)
	{
		try
		{
			await Task.Run(delegate
			{
				WriteSync(message, level);
			});
		}
		catch (Exception arg)
		{
			System.Diagnostics.Trace.WriteLine($"ERROR - ConsoleAppender Could not write to log file. {arg}");
		}
	}

	private void WriteSync(string message, LogLevel level)
	{
		try
		{
			switch (level)
			{
			case LogLevel.Error:
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("*******************");
				break;
			case LogLevel.Warning:
				Console.ForegroundColor = ConsoleColor.Yellow;
				break;
			case LogLevel.Info:
				Console.ForegroundColor = ConsoleColor.White;
				break;
			default:
				Console.ForegroundColor = ConsoleColor.Gray;
				break;
			}
			Console.WriteLine(message);
			Console.ForegroundColor = ConsoleColor.Gray;
		}
		catch (Exception arg)
		{
			System.Diagnostics.Trace.WriteLine($"ERROR - ConsoleAppender Could not writeSync to log file. {arg}");
		}
	}

	public void Dispose()
	{
	}
}
