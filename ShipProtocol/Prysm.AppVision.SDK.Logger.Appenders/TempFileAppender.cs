#define TRACE
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Prysm.AppVision.SDK.Logger.Appenders;

public class TempFileAppender : ILogAppender, IDisposable
{
	private readonly string _path;

	private bool _failed;

	public TempFileAppender(string name)
	{
		string path = Path.Combine(Path.GetTempPath(), "Logs");
		_path = Path.Combine(path, name + ".log");
	}

	public async Task Write(string message, LogLevel level)
	{
		try
		{
			if (!_failed)
			{
				File.AppendAllText(_path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + "\r\n");
			}
		}
		catch (Exception arg)
		{
			System.Diagnostics.Trace.WriteLine($"ERROR - Could not write to log file. {arg}");
			_failed = true;
		}
	}

	public void Dispose()
	{
	}
}
