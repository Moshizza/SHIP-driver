using System;
using System.Threading.Tasks;

namespace Prysm.AppVision.SDK.Logger.Appenders;

public interface ILogAppender : IDisposable
{
	Task Write(string message, LogLevel level);
}
