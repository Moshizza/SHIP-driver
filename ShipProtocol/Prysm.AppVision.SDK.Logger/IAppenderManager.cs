using System.Collections.Generic;
using Prysm.AppVision.SDK.Logger.Appenders;

namespace Prysm.AppVision.SDK.Logger;

public interface IAppenderManager
{
	List<AppenderInfo> Appenders { get; }

	void AddAppender(ILogAppender appender, LogLevel level, string name);

	void RemoveAppender(string name);
}
