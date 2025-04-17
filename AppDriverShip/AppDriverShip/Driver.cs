using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppDriverShip.Helpers;
using AppDriverShip.Model;
using Prysm.AppControl.SDK;
using Prysm.AppControl.SDK.AppControlDataServiceReference;
using Prysm.AppControl.SDK.AppVisionHistoDataServiceReference;
using Prysm.AppVision.Common;
using Prysm.AppVision.Data;
using Prysm.AppVision.SDK;
using Prysm.Security;

namespace AppDriverShip;

internal class Driver
{
	internal const string OEMCODE = "568";

	private List<SaltoServer> _devices = new List<SaltoServer>();

	internal ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>();

	private long _lastModificationId = -1L;

	private int downloadMonitoringInternalInMs = 5000;

	internal bool IsSaltoOEM { get; private set; }

	internal AppServerForDriver AppServer { get; set; }

	internal CancellationTokenSource Token { get; set; }

	internal Driver()
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		Trace.Info(Environment.CommandLine, ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 35);
		AppServer = new AppServerForDriver();
		((IRowStateManager<VariableRow, VariableState>)(object)((AppServer)AppServer).VariableManager).StateChanged += delegate(VariableState s)
		{
			Trace.Info($">app> {s.Name}={s.Value}", ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 38);
		};
		AppServer.Download += Download;
		AppServer.Refresh += Refresh;
		Trace.Logger.LogEvent += TraceOnLogEvent;
	}

	private void TraceOnLogEvent(LogEntry obj)
	{
		try
		{
			_ = obj.Level;
			AppServerForDriver appServer = AppServer;
			if (appServer != null)
			{
				ProtocolRow currentProtocol = appServer.CurrentProtocol;
				if (currentProtocol != null)
				{
					_ = currentProtocol.LogLevel;
					if (0 == 0)
					{
						switch (obj.Level)
						{
						case LogLevel.Error:
							((AppServer)AppServer).Log(0, obj.Message);
							break;
						case LogLevel.Info:
						case LogLevel.Warning:
							if (AppServer.CurrentProtocol.LogLevel >= 1)
							{
								((AppServer)AppServer).Log(1, obj.Message);
							}
							break;
						case LogLevel.Debug:
							if (AppServer.CurrentProtocol.LogLevel >= 2)
							{
								((AppServer)AppServer).Log(2, obj.Message);
							}
							break;
						case LogLevel.All:
							ObjectExtensions.DebugWriteLine((object)this, obj.Message, false, "TraceOnLogEvent");
							break;
						case LogLevel.None:
							break;
						}
						return;
					}
				}
			}
			((AppServer)AppServer).Log(1, obj.Message);
		}
		catch
		{
			((AppServer)AppServer).Log(1, obj.Message);
		}
	}

	public void Start()
	{
		try
		{
			Token = new CancellationTokenSource();
			string[] array = Environment.GetCommandLineArgs()[1].Split('@');
			string text = array[0];
			string text2 = ((array.Length > 1) ? array[1] : "");
			Trace.Info("Connecting to server " + text2, "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 120);
			((AppServer)AppServer).Open(text2);
			AppServer.Login(text);
			switch (AppServer.CurrentProtocol.LogLevel)
			{
			case 0:
				Trace.CurrentLogLevel = LogLevel.Error;
				break;
			case 1:
				Trace.CurrentLogLevel = LogLevel.Info;
				break;
			case 2:
				Trace.CurrentLogLevel = LogLevel.Debug;
				break;
			}
			if (!((AppServer)AppServer).CheckWarrantyDate(Timestamper.BuildAt))
			{
				Trace.Error("Driver version forbidden, extend warranty", "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 143);
				return;
			}
			IsSaltoOEM = ((AppServer)AppServer).GetLicenseInfo("CodesOEM").Contains("568");
			if (IsSaltoOEM)
			{
				Trace.Info("Salto OEM code detected.", "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 150);
			}
			VariableRow[] variablesByProtocol = AppServer.GetVariablesByProtocol();
			if (variablesByProtocol.Length == 0)
			{
				Trace.Warning("No Salto server node found for protocol '" + text + "'", "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 155);
			}
			Trace.Debug($"{variablesByProtocol.Count()} Salto server(s) found", "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 157);
			string text3 = CollectionExtensions.Join<string>(variablesByProtocol.Select((VariableRow v) => "$V." + ((TreeRow)v).Name + ".*"), "|");
			((AppServer)AppServer).AddFilterNotifications(text3);
			((AppServer)AppServer).AddFilterNotifications("Variable.StateType=1");
			((AppServer)AppServer).StartNotifications(false, true, 0);
			PrysmSdkHelper.ProtocolName = text;
			PrysmSdkHelper.Driver = this;
			VariableRow[] array2 = variablesByProtocol;
			foreach (VariableRow val in array2)
			{
				try
				{
					SaltoServer saltoServer = new SaltoServer(this, val);
					saltoServer.Start();
					_devices.Add(saltoServer);
				}
				catch (Exception ex)
				{
					AddAlarm(ex, val);
				}
			}
			AppServer.ProtocolSynchronized();
			Trace.Info("Protocol " + text + " synchronized", "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 184);
			MonitoringLoop();
			Trace.Debug("Driver started", "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 192);
		}
		catch (Exception)
		{
			throw;
		}
	}

	public void Stop()
	{
		Trace.Info("Stopping", "Stop", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 203);
		try
		{
			if (Task.Run(delegate
			{
				Token.Cancel();
				foreach (SaltoServer device in _devices)
				{
					device.Stop();
				}
			}).Wait(TimeSpan.FromSeconds(10.0)))
			{
				Trace.Info("Properly stopped", "Stop", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 216);
			}
			else
			{
				Trace.Warning("Stopped timed out", "Stop", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 220);
			}
			_devices.Clear();
			if (((AppServer)AppServer).IsConnected)
			{
				((AppServer)AppServer).Close();
			}
			Trace.Info("Stopped", "Stop", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 230);
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "Stop", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 234);
		}
	}

	private async Task MonitoringLoop()
	{
		while (!Token.IsCancellationRequested)
		{
			await DownloadModificationsAsync();
			await Wait(downloadMonitoringInternalInMs, Token);
		}
	}

	internal static async Task Wait(int ms, CancellationTokenSource stop = null)
	{
		_ = 1;
		try
		{
			if (stop == null)
			{
				await Task.Delay(ms);
			}
			if (stop != null && !stop.IsCancellationRequested)
			{
				await Task.Delay(ms, stop.Token);
			}
		}
		catch (TaskCanceledException)
		{
		}
		catch (Exception ex2)
		{
			Trace.Error(ex2, null, "Wait", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 264);
		}
	}

	internal void Download(string varName, string parameters)
	{
		Trace.Info("Download: " + varName + " / " + parameters, "Download", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 270);
		try
		{
			_devices.FirstOrDefault((SaltoServer x) => ((TreeRow)x.RootVar).Name == varName)?.DriverDownload(varName, parameters);
		}
		catch (Exception ex)
		{
			AddAlarm(ex);
		}
	}

	public void Refresh(string message)
	{
		Trace.Info("Refresh: " + message, "Refresh", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 283);
		try
		{
			foreach (SaltoServer device in _devices)
			{
				device.Refresh(message);
			}
		}
		catch (Exception ex)
		{
			AddAlarm(ex);
		}
	}

	internal void AddAlarm(Exception ex, VariableRow row = null)
	{
		try
		{
			Trace.Error(ex, "[" + ((row != null) ? ((TreeRow)row).Name : null) + "]", "AddAlarm", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 300);
			((AppServer)AppServer).AlarmManager.AddAlarm2(10, ex.Message, (row != null) ? ((TreeRow)row).FullDescription : null, ex.ToString(), (row != null) ? row.AreaDescription : null, (row != null) ? row.FullGroupDescriptions : null, "", "", false);
		}
		catch
		{
		}
	}

	internal void SetVariableNoCache(string name, object value, string info = null, string operation = null, DateTimeOffset date = default(DateTimeOffset))
	{
		try
		{
			Trace.Info($"<drv< {name}={value}\t{info}", "SetVariableNoCache", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 311);
			((AppServer)AppServer).VariableManager.Set(name, value, info, operation, date, 0, 0);
		}
		catch
		{
		}
	}

	internal void SetVariableNoCacheWithParameters(string name, object value, string info, NameValue[] parameters)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		try
		{
			Trace.Info($"<drv< {name}={value}\twith sparams", "SetVariableNoCacheWithParameters", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 321);
			List<NameValue> list = new List<NameValue>(parameters)
			{
				new NameValue("Info", (object)info)
			};
			((AppServer)AppServer).VariableManager.SetExt(name, value, list.ToArray());
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "SetVariableNoCacheWithParameters", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 325);
		}
	}

	internal void SetVariable(string name, object value, string info = null, string operation = null, DateTimeOffset date = default(DateTimeOffset))
	{
		if (!_cache.TryGetValue(name, out var value2) || value == null || !value.Equals(value2))
		{
			_cache[name] = value;
			SetVariableNoCache(name, value, info, operation, date);
		}
	}

	internal void SetVariableWithParameters(string name, object value, string info, NameValue[] parameters)
	{
		if (!_cache.TryGetValue(name, out var value2) || value == null || !value.Equals(value2))
		{
			_cache[name] = value;
			SetVariableNoCacheWithParameters(name, value, info, parameters);
		}
	}

	private async Task DownloadModificationsAsync()
	{
		await Task.Run(delegate
		{
			try
			{
				ModifRow[] lastModifs = GetLastModifs();
				CommandDownloadList commandDownloadList = GetCommandDownloadList(lastModifs);
				foreach (SaltoServer device in _devices)
				{
					commandDownloadList.Variables.Contains(((TreeRow)device.RootVar).Id);
					if (commandDownloadList.Modifs.Any())
					{
						device.UpdateModifications(commandDownloadList.Modifs);
					}
					if (commandDownloadList.Users.Count > 0 || commandDownloadList.Timetables.Count > 0 || commandDownloadList.Holidays || commandDownloadList.Parameters)
					{
						device.UpdateModifications(commandDownloadList);
					}
				}
			}
			catch (Exception ex)
			{
				Trace.Error(ex, null, "DownloadModificationsAsync", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 373);
			}
		});
	}

	private ModifRow[] GetLastModifs()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		if (PrysmSdkHelper.Datalayer == null)
		{
			PrysmSdkHelper.Driver = this;
		}
		Datalayer val = new Datalayer((AppServer)(object)AppServer);
		if (_lastModificationId < 0)
		{
			_lastModificationId = val.GetLastModificationId();
			return (ModifRow[])(object)new ModifRow[0];
		}
		if (_lastModificationId > int.MaxValue)
		{
			Trace.Error("ERROR !! -> Last modification id too big", "GetLastModifs", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Driver.cs", 396);
			return (ModifRow[])(object)new ModifRow[0];
		}
		ModifRow[] lastModifications = val.GetLastModifications((int)_lastModificationId);
		if (lastModifications.Length == 0)
		{
			return (ModifRow[])(object)new ModifRow[0];
		}
		_lastModificationId = lastModifications.Last().Id;
		return lastModifications;
	}

	private CommandDownloadList GetCommandDownloadList(ModifRow[] modifs)
	{
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		CommandDownloadList commandDownloadList = new CommandDownloadList();
		foreach (ModifRow val in modifs)
		{
			switch (val.Table)
			{
			case "Apc_Badges":
			case "Apc_People":
			case "Apc_PersonRights":
			case "Apc_PersonProfiles":
				if (val.SourceId > 0)
				{
					if (!commandDownloadList.Modifs.Contains(val))
					{
						commandDownloadList.Modifs.Add(val);
					}
					if (!commandDownloadList.Users.Contains(val.SourceId))
					{
						commandDownloadList.Users.Add(val.SourceId);
					}
				}
				break;
			case "Apc_AccessProfileRights":
			{
				Person[] peopleByDepartmentId = new Datalayer((AppServer)(object)AppServer).GetPeopleByProfileId(val.SourceId);
				foreach (Person val3 in peopleByDepartmentId)
				{
					if (!commandDownloadList.Users.Contains(val3.Id))
					{
						commandDownloadList.Users.Add(val3.Id);
					}
				}
				break;
			}
			case "Apc_Categories":
			{
				Person[] peopleByDepartmentId = new Datalayer((AppServer)(object)AppServer).GetPeopleByCategoryId(val.SourceId);
				foreach (Person val4 in peopleByDepartmentId)
				{
					if (!commandDownloadList.Users.Contains(val4.Id))
					{
						commandDownloadList.Users.Add(val4.Id);
					}
				}
				break;
			}
			case "Apc_Departments":
			{
				Person[] peopleByDepartmentId = new Datalayer((AppServer)(object)AppServer).GetPeopleByDepartmentId(val.SourceId);
				foreach (Person val2 in peopleByDepartmentId)
				{
					if (!commandDownloadList.Users.Contains(val2.Id))
					{
						commandDownloadList.Users.Add(val2.Id);
					}
				}
				break;
			}
			case "Variables":
			{
				int type = val.Type;
				if ((uint)type <= 1u && !commandDownloadList.Variables.Contains(val.SourceId))
				{
					commandDownloadList.Variables.Add(val.SourceId);
				}
				break;
			}
			case "Timetables":
			{
				int type = val.Type;
				if ((uint)type <= 1u && !commandDownloadList.Timetables.Contains(val.SourceId))
				{
					commandDownloadList.Timetables.Add(val.SourceId);
				}
				break;
			}
			case "Holidays":
				commandDownloadList.Holidays = true;
				break;
			}
		}
		return commandDownloadList;
	}
}
