using System;
using System.IO;
using System.Threading.Tasks;
using Prysm.AppVision.Common;

namespace Prysm.AppVision.SDK.Logger.Appenders;

public class AppAppender : ILogAppender, IDisposable
{
	private string _logPeriod;

	private readonly string _filename;

	private string _logFilename;

	private DateTime _currentLogPeriodDateTime;

	public AppAppender(string logPeriod, string filename = null)
	{
		_logPeriod = logPeriod;
		if (string.IsNullOrEmpty(filename))
		{
			filename = Path.GetFileNameWithoutExtension(Helper.GetEntryAssembly().Location);
		}
		_currentLogPeriodDateTime = DateTime.UtcNow.AddHours(1.0);
		_filename = Path.Combine(Helper.PathLogFiles, filename);
		_logFilename = GetLogFilename(_logPeriod, _filename);
	}

	public void Dispose()
	{
	}

	public async Task Write(string message, LogLevel level)
	{
		await Task.Run(delegate
		{
			WriteSync(message);
		}).ConfigureAwait(continueOnCapturedContext: false);
	}

	private void WriteSync(string message)
	{
		if (_currentLogPeriodDateTime < DateTime.UtcNow || string.IsNullOrWhiteSpace(_logFilename))
		{
			_currentLogPeriodDateTime = DateTime.UtcNow.AddHours(1.0);
			_logFilename = GetLogFilename(_logPeriod, _filename);
		}
		try
		{
			File.AppendAllText(_logFilename, message);
		}
		catch (Exception)
		{
		}
	}

	internal void SetLogPeriod(string logPeriod)
	{
		_logPeriod = logPeriod;
		if (!string.IsNullOrEmpty(_filename))
		{
			_logFilename = GetLogFilename(logPeriod, _filename);
		}
	}

	private string GetLogFilename(string logPeriod, string filename)
	{
		string text = logPeriod switch
		{
			"YM" => $"{filename}_{DateTime.Now:yyMM}.log", 
			"YMD" => $"{filename}_{DateTime.Now:yyMMdd}.log", 
			"YMDH" => $"{filename}_{DateTime.Now:yyMMddHH}.log", 
			_ => filename + ".log", 
		};
		int num = 1;
		while (true)
		{
			FileInfo fileInfo = new FileInfo(text);
			if (!fileInfo.Exists || fileInfo.Length < 50000000)
			{
				break;
			}
			text = logPeriod switch
			{
				"YM" => $"{filename}_{DateTime.Now:yyMM}.{num}.log", 
				"YMD" => $"{filename}_{DateTime.Now:yyMMdd}.{num}.log", 
				"YMDH" => $"{filename}_{DateTime.Now:yyMMddHH}.{num}.log", 
				_ => $"{filename}.{num}.log", 
			};
			num++;
		}
		return text;
	}
}
