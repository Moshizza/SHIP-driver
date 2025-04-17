using System;

namespace Prysm.AppVision.SDK.Logger;

public interface ILog
{
	event Action<LogEntry> LogEvent;

	void Debug(string message, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0);

	void Debug(Exception ex, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0);

	void Info(string message, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0);

	void Warn(string message, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0);

	void Warn(string message, Exception ex, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0);

	void Warn(Exception ex, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0);

	void Error(string message, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0);

	void Error(string message, Exception ex, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0);

	void Error(Exception ex, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0);
}
