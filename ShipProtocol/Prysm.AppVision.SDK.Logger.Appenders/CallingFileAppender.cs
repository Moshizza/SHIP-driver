#define TRACE
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Prysm.AppVision.SDK.Logger.Appenders;

public class CallingFileAppender : ILogAppender, IDisposable
{
	private readonly string _path;

	private Timer timer;

	private static StringBuilder _builder = new StringBuilder();

	public CallingFileAppender(string fullFilename)
	{
		if (string.IsNullOrWhiteSpace(fullFilename))
		{
			fullFilename = Assembly.GetExecutingAssembly().Location;
		}
		_path = Path.Combine(Path.GetDirectoryName(fullFilename), Path.GetFileNameWithoutExtension(fullFilename) + $"_{DateTime.Now:yyMMdd}.log");
		PrepareAppender();
		InitializeTimer();
	}

	private void PrepareAppender()
	{
		try
		{
			string directoryName = Path.GetDirectoryName(_path);
			if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
		}
		catch (Exception arg)
		{
			System.Diagnostics.Trace.WriteLine($"ERROR - Could not initialize to log file. {arg}");
		}
	}

	private void InitializeTimer()
	{
		if (timer == null)
		{
			timer = new Timer();
			timer.Interval = 200.0;
			timer.Elapsed += TimerOnElapsed;
			timer.Enabled = true;
		}
	}

	public async Task Write(string message, LogLevel level)
	{
		try
		{
			if (level == LogLevel.Error)
			{
				_builder.Append("*************************************\r\n");
			}
			_builder.Append(message + Environment.NewLine);
			if (level == LogLevel.Error)
			{
				_builder.Append(Environment.NewLine);
			}
		}
		catch (Exception arg)
		{
			System.Diagnostics.Trace.WriteLine($"ERROR - Could not write to log file. {arg}");
		}
	}

	private void TimerOnElapsed(object sender, ElapsedEventArgs e)
	{
		WriteFile();
	}

	private void WriteFile()
	{
		try
		{
			if (_builder.Length < 1)
			{
				return;
			}
			using (FileStream stream = File.Open(_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
			{
				using StreamWriter streamWriter = new StreamWriter(stream);
				streamWriter.Write(_builder.ToString());
			}
			_builder.Clear();
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
		}
	}

	public void Dispose()
	{
		try
		{
			if (timer != null)
			{
				timer.Elapsed -= TimerOnElapsed;
				timer.Enabled = false;
				timer.Stop();
				timer.Dispose();
				timer = null;
			}
		}
		catch (Exception value)
		{
			System.Diagnostics.Trace.WriteLine("Can not dispose BinFileAppender");
			Console.WriteLine(value);
		}
	}
}
