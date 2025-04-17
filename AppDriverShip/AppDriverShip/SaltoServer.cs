using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apis.Messaging;
using Apis.Salto.Ship.Api;
using Apis.Salto.Ship.Model;
using AppDriverShip.Business;
using AppDriverShip.Helpers;
using AppDriverShip.Model;
using AppDriverShip.Ship;
using Prysm.AppControl.SDK;
using Prysm.AppControl.SDK.AppControlDataServiceReference;
using Prysm.AppControl.SDK.AppVisionHistoDataServiceReference;
using Prysm.AppVision.Common;
using Prysm.AppVision.Data;
using Prysm.AppVision.SDK;

namespace AppDriverShip;

internal class SaltoServer : IParentMessager<string>
{
	private readonly VariableRow _rootVar;

	internal const int MAX_READER = 4;

	internal const int MAX_HOLIDAY = 24;

	internal const int MAX_TIMETABLE = 48;

	internal const int MAX_USER = 10000;

	internal const int MAX_PROFILE = 256;

	private int _alertBatteryLevel = 30;

	private int _lastAuditTrailId;

	private Driver _driver;

	internal CancellationTokenSource Token = new CancellationTokenSource();

	private readonly ConcurrentDictionary<string, Door> _doorsStatus = new ConcurrentDictionary<string, Door>();

	private ShipReceiver _shipReceiver;

	private VariableRow[] _doorsRows;

	private ChannelService _channel;

	private AppServerForDriver _appServer;

	private string[] _readerNames;

	private string[] _relays;

	private readonly Cache _cache;

	private int _maxValidityInHours;

	private readonly List<VariableRow> _rows = new List<VariableRow>();

	private bool _useExtIds;

	private bool _areUsersInSalto;

	private CommStatus _commStatus;

	private Dictionary<string, DateTime> _lastOnlineDoorUpdate = new Dictionary<string, DateTime>();

	private Dictionary<string, DateTime> _lastFoundWarning = new Dictionary<string, DateTime>();

	private Timer _housekeepingTimer;

	private ConcurrentDictionary<string, DateTime> _logLimiter = new ConcurrentDictionary<string, DateTime>();

	public CommStatus Comm
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return _commStatus;
		}
		set
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Expected I4, but got Unknown
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			_driver.SetVariable(((TreeRow)_rootVar).Name + ".Comm", (int)value);
			_commStatus = value;
		}
	}

	public VariableRow RootVar => _rootVar;

	internal SaltoServer(Driver driver, VariableRow variable)
	{
		Trace.Debug("Ctor " + ((variable != null) ? ((TreeRow)variable).Name : null), ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 57);
		_driver = driver;
		_rootVar = variable;
		int parameter = Helper.GetParameter<int>("SaltoHostPort", variable.FullParameters, 0);
		if (parameter <= 0)
		{
			throw new ArgumentNullException("FullParameters", "Missing or invalid parameter 'SaltoHostPort'");
		}
		_useExtIds = Helper.GetParameter<bool>("UseExtIds", _rootVar.Parameters, true);
		_appServer = driver.AppServer;
		_cache = new Cache(Path.Combine(Helper.PathDocumentAppVision, "cache"), _rootVar);
		_alertBatteryLevel = Helper.GetParameter<int>("AlertBatteryLevel", _rootVar.Parameters, 30);
		if (_alertBatteryLevel < 1)
		{
			_alertBatteryLevel = 30;
		}
		_maxValidityInHours = Helper.GetParameter<int>("MaxValidityInHours", _rootVar.Parameters, 720);
		Comm = (CommStatus)0;
		GetLastAuditTrailId();
		_channel = new ChannelService(driver, _rootVar.Address, _rootVar.Parameters, _lastAuditTrailId);
		_areUsersInSalto = Helper.GetParameter<bool>("UsersInSalto", variable.FullParameters, false);
		Trace.Info("Start ShipReceiver for " + ((TreeRow)variable).Name, ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 89);
		_shipReceiver = new ShipReceiver(parameter, this);
	}

	internal void Start()
	{
		Trace.Info("Start SaltoServer " + ((TreeRow)_rootVar).Name, "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 118);
		Token = new CancellationTokenSource();
		_housekeepingTimer = new Timer(Housekeeping, null, TimeSpan.FromHours(1.0), TimeSpan.FromHours(1.0));
		CollectionExtensions.ReplaceAll<VariableRow>((ICollection<VariableRow>)_rows, (IEnumerable<VariableRow>)((IRowStateManager<VariableRow, VariableState>)(object)((AppServer)_driver.AppServer).VariableManager).GetRowsByName(((TreeRow)_rootVar).Name + ".*"));
		InitializeAllDoors();
		Task.Run((Action)_shipReceiver.Start);
		_channel.ConnectionEventReceived += Channel_ConnectionEventReceived;
		_channel.EventCardReceived += Channel_EventCardReceived;
		_channel.EventDoorReceived += Channel_EventDoorReceived;
		_channel.EventDoorUpdateReceived += Channel_EventDoorUpdateReceived;
		_channel.EventOperatorReceived += Channel_EventOperatorReceived;
		_channel.LastAuditTrailIdReceived += Channel_LastAuditTrailIdReceived;
		_channel.EventStreamReceived += Channel_EventStreamReceived;
		_channel.EventOnlineDoor += ChannelOnEventOnlineDoor;
		_channel.HookEventReceived += Channel_HookEventReceived;
		_channel.Start();
		((IRowStateManager<VariableRow, VariableState>)(object)((AppServer)_driver.AppServer).VariableManager).StateChanged += VariableManager_StateChanged;
		Trace.Info("Device " + ((TreeRow)_rootVar).Name + " started > Update cache", "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 140);
		DriverDownload(((TreeRow)_rootVar).Name, "People,Rights,Timetables,Holidays,Parameters");
		Trace.Info("Device " + ((TreeRow)_rootVar).Name + " Cache ok", "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 142);
	}

	private void VariableManager_StateChanged(VariableState state)
	{
		if (!state.Name.ToLower().StartsWith(((TreeRow)_rootVar).Name.ToLower()) || !state.Name.ToLower().EndsWith("cmd"))
		{
			return;
		}
		Trace.Debug($"VariableStateChanged: {state.Name}={state.Value}", "VariableManager_StateChanged", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 154);
		if (_driver.IsSaltoOEM && state.Name.ToLower().Contains("onlinerelaycmd"))
		{
			RunOnlineRelay(state);
			return;
		}
		VariableRow val = _rows.FirstOrDefault((VariableRow x) => ((TreeRow)x).Name == state.Name);
		if (val == null)
		{
			Trace.Warning("Can not find row: " + state.Name, "VariableManager_StateChanged", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 169);
			return;
		}
		if (string.IsNullOrWhiteSpace(val.FullAddress))
		{
			Trace.Warning("No ExtId in Address filed for row: " + ((TreeRow)val).Name, "VariableManager_StateChanged", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 174);
			return;
		}
		if (_driver.IsSaltoOEM && state.Name.Contains(".AlarmOutputs.") && state.Name.EndsWith(".Cmd"))
		{
			string parameter = val.GetParameter("RelayName");
			if (string.IsNullOrWhiteSpace(parameter))
			{
				Trace.Error("Can not find parameter RelayName in row: " + ((val != null) ? ((TreeRow)val).Name : null), "VariableManager_StateChanged", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 186);
				return;
			}
			_channel.CommandOnlineRelays(new string[1] { parameter }, state.ValueToBool());
			return;
		}
		string fullAddress = val.FullAddress;
		string text = Helper.GetParameter("Type", val.FullParameters, (string)null) ?? Helper.GetParameter("DoorType", val.FullParameters, (string)null);
		Trace.Info("VariableStateChanged: type=" + text + " // id=" + fullAddress + " // (" + state.Name + ")", "VariableManager_StateChanged", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 197);
		if (text == "zones")
		{
			RunZoneCommands(fullAddress, (int)state.Value);
		}
		else
		{
			RunDoorCommands(fullAddress, (int)state.Value);
		}
	}

	private async Task RunOnlineRelay(VariableState state)
	{
		if (string.IsNullOrWhiteSpace((state == null) ? null : state.Value?.ToString()))
		{
			Trace.Debug("RunOnlineRelay > state value is null", "RunOnlineRelay", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 210);
			return;
		}
		if (!state.Value.ToString().Contains("|"))
		{
			Trace.Debug("RunOnlineRelay > need a pipe '|'", "RunOnlineRelay", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 215);
			return;
		}
		string[] array = state.Value.ToString().Split('|');
		string[] array2 = array[1].Split(',');
		bool flag = array[0].ToLower() == "1";
		Trace.Info(string.Format("Send cmd to ship > {0} = {1}", CollectionExtensions.Join<string>((IEnumerable<string>)array2, ","), flag), "RunOnlineRelay", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 222);
		await _channel.CommandOnlineRelays(array2, flag);
	}

	private async Task RunDoorCommands(string extId, int value)
	{
		Trace.Info($"RunDoorCommands id={extId}  // value={value} // useExtId={_useExtIds}", "RunDoorCommands", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 228);
		switch (value)
		{
		case 0:
			await _channel.OpenOnlineDoorCmd(extId);
			break;
		case 1:
			await _channel.CloseOnlineDoorCmd(extId);
			break;
		case 2:
			await _channel.EmergencyOpenOnlineDoorCmd(extId);
			break;
		case 3:
			await _channel.EmergencyCloseOnlineDoorCmd(extId);
			break;
		case 4:
			await _channel.EndOfEmergencyCmd(extId);
			break;
		case 5:
			await _channel.StartOfficeModeCmd(extId);
			break;
		case 6:
			await _channel.EndOfficeModeCmd(extId);
			break;
		default:
			Trace.Warning($"Unknown RunDoorCommands {extId} => {value}", "RunDoorCommands", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 258);
			break;
		}
	}

	private async Task RunZoneCommands(string extId, int value)
	{
		switch (value)
		{
		case 0:
			await _channel.OpenOnlineZoneCmd(extId);
			break;
		case 1:
			await _channel.CloseOnlineZoneCmd(extId);
			break;
		case 2:
			await _channel.EmergencyOpenOnlineZoneCmd(extId);
			break;
		case 3:
			await _channel.EmergencyCloseOnlineZoneCmd(extId);
			break;
		case 4:
			await _channel.EndOfEmergencyOnlineZoneCmd(extId);
			break;
		case 5:
			await _channel.StartOfficeModeOnlineZoneCmd(extId);
			break;
		case 6:
			await _channel.EndOfficeModeOnlineZoneCmd(extId);
			break;
		default:
			Trace.Debug($"Unknown RunZoneCommands {extId} => {value}", "RunZoneCommands", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 294);
			break;
		}
	}

	internal void Stop()
	{
		Trace.Info("Stop ship", "Stop", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 304);
		_shipReceiver.Stop();
		Trace.Debug("****** Stop device Thread ******", "Stop", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 307);
		try
		{
			((IRowStateManager<VariableRow, VariableState>)(object)((AppServer)_driver.AppServer).VariableManager).StateChanged -= VariableManager_StateChanged;
			_channel.ConnectionEventReceived -= Channel_ConnectionEventReceived;
			_channel.EventCardReceived -= Channel_EventCardReceived;
			_channel.EventDoorReceived -= Channel_EventDoorReceived;
			_channel.EventDoorUpdateReceived -= Channel_EventDoorUpdateReceived;
			_channel.EventOperatorReceived -= Channel_EventOperatorReceived;
			_channel.LastAuditTrailIdReceived -= Channel_LastAuditTrailIdReceived;
			_channel.EventStreamReceived -= Channel_EventStreamReceived;
			_channel.EventOnlineDoor -= ChannelOnEventOnlineDoor;
			_channel.HookEventReceived -= Channel_HookEventReceived;
			_channel.Stop();
			_housekeepingTimer?.Dispose();
		}
		catch (Exception ex)
		{
			Trace.Warning("Exception: " + ex.Message, "Stop", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 330);
		}
	}

	private void Channel_LastAuditTrailIdReceived(int obj)
	{
		SetLastAuditTrailId(obj);
	}

	private void Channel_EventOperatorReceived(EventOperator obj)
	{
		Trace.Debug(string.Format("{0} - {1} / {2}", "Channel_EventOperatorReceived", obj.DoorID, obj.Operator), "Channel_EventOperatorReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 346);
	}

	private void Channel_EventDoorUpdateReceived(EventDoorUpdate obj)
	{
		string doorName = ((!_useExtIds) ? obj?.DoorName : obj?.ExtDoorID);
		try
		{
			if (!_doorsStatus.ContainsKey(doorName) && _logLimiter.TryGetValue(obj.ExtDoorID, out var value) && value.AddDays(5.0) < DateTime.Now)
			{
				_logLimiter[obj.ExtDoorID] = DateTime.Now;
				Trace.Warning($"EventDoorUpdateReceived - CHECK YOUR CONFIG - DoorStatus does not contains {doorName} (useExtId: {_useExtIds}) / extDoorId: {obj.ExtDoorID} / obj doorName: {obj.DoorName}. Can't update status. Example of doorName expected: {_doorsStatus.FirstOrDefault().Key} for row:{_doorsStatus.FirstOrDefault().Value?.RowName} with saltoNameOrId: {_doorsStatus.FirstOrDefault().Value?.SaltoNameOrId}", "Channel_EventDoorUpdateReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 366);
			}
			if (!_doorsStatus.ContainsKey(doorName))
			{
				return;
			}
			if (!string.IsNullOrWhiteSpace(_doorsStatus[doorName].RowName))
			{
				VariableRow val = _rows.FirstOrDefault((VariableRow x) => ((TreeRow)x).Name == _doorsStatus[doorName].RowName);
				if (val != null)
				{
					_doorsStatus[doorName].IsOnline = Helper.GetParameter<bool>("IsOnline", val.Parameters, false);
				}
			}
			if (!_doorsStatus[doorName].IsOnline)
			{
				bool flag = obj.BatteryStatus < _alertBatteryLevel && obj.BatteryStatus >= 0;
				_doorsStatus[doorName].LowBattery = flag;
				_doorsStatus[doorName].UpdateRequired = obj.NeedUpdate;
				_doorsStatus[doorName].BatteryLevel = obj.BatteryStatus;
				_driver.SetVariable(_doorsStatus[doorName].RowName + ".BatteryLevel", obj.BatteryStatus, "EventDoorUpdate");
				_driver.SetVariable(_doorsStatus[doorName].RowName + ".NeedUpdate", obj.NeedUpdate, "EventDoorUpdate");
				_driver.SetVariable(_doorsStatus[doorName].RowName + ".LowBattery", flag, $"threshold: {_alertBatteryLevel}% | eventDoorBattery {obj.BatteryStatus} ");
				long status = _doorsStatus[doorName].GetStatus();
				_driver.SetVariable(_doorsStatus[doorName].RowName + ".Status", status, $"from EventDoorUpdate | need update: {_doorsStatus[doorName].UpdateRequired} | low battery: {_doorsStatus[doorName].LowBattery} ({_doorsStatus[doorName].BatteryLevel}%)");
				UpdateSaltoDbDoor(obj.ExtDoorID, doorName);
			}
		}
		catch (Exception ex)
		{
			Trace.Error($"EventDoorUpdateReceived exception for {obj?.DoorName} / {obj?.ExtDoorID} (useExtId: {_useExtIds})=> {doorName}", "Channel_EventDoorUpdateReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 399);
			Trace.Debug(ex, null, "Channel_EventDoorUpdateReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 400);
		}
	}

	private void UpdateSaltoDbDoor(string doorId, string doorName)
	{
		try
		{
			SaltoDBDoor saltoDBDoor = (_useExtIds ? _channel.GetDoor(doorId) : _channel.GetDoorByName(doorName));
			if (saltoDBDoor == null)
			{
				Trace.Debug("Can not find salto door id=" + doorId + "   doorname=" + doorName, "UpdateSaltoDbDoor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 411);
				return;
			}
			if (!_doorsStatus.ContainsKey(doorName))
			{
				Trace.Debug("Can not find doorStatus doorname=" + doorName, "UpdateSaltoDbDoor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 417);
				return;
			}
			saltoDBDoor.BatteryStatus = _doorsStatus[doorName].BatteryLevel;
			saltoDBDoor.UpdateRequired = (_doorsStatus[doorName].UpdateRequired ? 1 : 0);
		}
		catch (Exception ex)
		{
			Trace.Debug("Exception in Device.UpdateSaltoDbDoor for " + doorId + " / " + doorName + ": " + ex.Message, "UpdateSaltoDbDoor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 426);
			Trace.Error(ex, null, "UpdateSaltoDbDoor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 427);
		}
	}

	private void Channel_EventDoorReceived(EventDoor obj)
	{
		string text = "";
		string operationDescription = SaltoSdkHelper.GetOperationDescription(obj.EventID);
		string operationGroup = SaltoSdkHelper.GetOperationGroup(obj.EventID);
		try
		{
			SaltoDBDoor saltoDBDoor;
			if (_useExtIds)
			{
				saltoDBDoor = _channel.GetDoor(obj.DoorID);
				text = GetDoorRowFullNameFromDoorExtId(obj?.DoorID);
			}
			else
			{
				saltoDBDoor = _channel.GetDoorByName(obj.DoorID);
				text = GetDoorRowFullNameFromDoorId(obj?.DoorID);
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				VariableRow obj2 = _rows.FirstOrDefault((VariableRow x) => x.Address == obj.DoorID);
				text = ((obj2 != null) ? ((TreeRow)obj2).Name : null);
			}
			if (saltoDBDoor == null)
			{
				Trace.Debug("Channel_EventDoorReceived - door not found: " + obj.DoorID, "Channel_EventDoorReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 455);
				return;
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				Trace.Debug("Channel_EventDoorReceived - Unknown door " + saltoDBDoor.Name + " / " + obj.DoorID, "Channel_EventDoorReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 461);
				return;
			}
			Trace.Debug($"Event for {text} ({obj.DoorID}) => {obj.EventID} = {operationGroup} / {operationDescription} ", "Channel_EventDoorReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 464);
			_driver.SetVariable(((TreeRow)_rootVar).Name + ".Trace", text + " => " + operationDescription + " (" + operationGroup + ")", "");
			if (!_doorsStatus.ContainsKey(obj.DoorID))
			{
				Trace.Debug($"EventDoorReceived - DoorStatus does not contains {obj.DoorID} (useExtId: {_useExtIds}) / doorName: {text}. Can't update status. Example of doorName expected: {_doorsStatus.FirstOrDefault().Key} for row:{_doorsStatus.FirstOrDefault().Value?.RowName} with saltoNameOrId: {_doorsStatus.FirstOrDefault().Value?.SaltoNameOrId}", "Channel_EventDoorReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 470);
			}
			else
			{
				if (!_doorsStatus[obj.DoorID].IsOnline)
				{
					UpdateOfflineDoor(obj, text, saltoDBDoor);
				}
				if (_doorsStatus[obj.DoorID].IsOnline)
				{
					UpdateOnlineDoor(obj, operationDescription, text);
				}
			}
		}
		catch (Exception ex)
		{
			Trace.Debug($"Device.Channel_EventDoorReceived for doorid={obj?.DoorID} as extId: {_useExtIds} (doorname={text}) - Exception: {ex.Message}", "Channel_EventDoorReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 491);
			Trace.Error(ex, null, "Channel_EventDoorReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 492);
		}
	}

	private void UpdateOfflineDoor(EventDoor obj, string doorName, SaltoDBDoor saltoDoor)
	{
		try
		{
			if (!_doorsStatus[obj.DoorID].IsOnline)
			{
				if (_rows.Any((VariableRow x) => ((TreeRow)x).Name == doorName + ".BatteryLevel"))
				{
					_driver.SetVariable(doorName + ".BatteryLevel", saltoDoor.BatteryStatus, "eventDoor");
				}
				_doorsStatus[obj.DoorID].UpdateRequired = saltoDoor.UpdateRequired == 1;
				_doorsStatus[obj.DoorID].LowBattery = saltoDoor.BatteryStatus < _alertBatteryLevel;
				_doorsStatus[obj.DoorID].BatteryLevel = saltoDoor.BatteryStatus;
				if (_rows.Any((VariableRow x) => ((TreeRow)x).Name == doorName + ".LowBattery"))
				{
					_driver.SetVariable(doorName + ".LowBattery", saltoDoor.BatteryStatus < _alertBatteryLevel, $"threshold: {_alertBatteryLevel}% | eventDoor");
				}
				long status = _doorsStatus[doorName].GetStatus();
				if (_rows.Any((VariableRow x) => ((TreeRow)x).Name == _doorsStatus[obj.DoorID].RowName + ".Status"))
				{
					_driver.SetVariable(_doorsStatus[obj.DoorID].RowName + ".Status", status, $"from EventDoor | need update: {_doorsStatus[doorName].UpdateRequired} | low battery: {_doorsStatus[doorName].LowBattery}");
				}
				else
				{
					Trace.Debug($"CAN NOT FIND STATUS ROW for {_doorsStatus[obj.DoorID].RowName}.Status => ({status}). ", "UpdateOfflineDoor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 518);
				}
			}
		}
		catch (Exception ex)
		{
			Trace.Debug($"Channel_EventDoorReceived => Exception while updating eventDoor: extid={obj?.DoorID} / ev={obj?.EventID}", "UpdateOfflineDoor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 523);
			Trace.Debug(ex.Message, "UpdateOfflineDoor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 524);
		}
	}

	private void UpdateOnlineDoor(EventDoor obj, string opDesc, string doorName)
	{
		try
		{
			if (!_doorsStatus[obj.DoorID].IsOnline)
			{
				return;
			}
			byte[] bytes = BitConverter.GetBytes(_doorsStatus[obj.DoorID].OnlineStatus2);
			switch (obj.EventID)
			{
			case 16:
			case 17:
			case 18:
			case 19:
			case 20:
			case 21:
			case 22:
			case 24:
			case 25:
			case 26:
			case 27:
			case 28:
			case 29:
				_doorsStatus[obj.DoorID].OnlineStatus = 0;
				_driver.SetVariable(doorName + ".Status", 1, opDesc);
				return;
			case 33:
			case 34:
			case 35:
			case 36:
				_doorsStatus[obj.DoorID].OnlineStatus = 1;
				_driver.SetVariable(doorName + ".Status", 2, opDesc);
				return;
			case 62:
				_doorsStatus[obj.DoorID].OnlineStatus = 2;
				_driver.SetVariable(doorName + ".Status", 3, opDesc);
				return;
			case 63:
				_doorsStatus[obj.DoorID].OnlineStatus = 1;
				_driver.SetVariable(doorName + ".Status", 2, opDesc);
				return;
			case 48:
			case 80:
				if (bytes.Length > 1)
				{
					bytes[1] = 0;
				}
				_driver.SetVariable(doorName + ".Comm", bytes.ToLong(), opDesc);
				return;
			case 47:
			case 79:
			case 115:
				if (bytes.Length > 1)
				{
					bytes[1] = 1;
				}
				_driver.SetVariable(doorName + ".Comm", bytes.ToLong(), opDesc);
				return;
			case 61:
				if (bytes.Length > 3)
				{
					bytes[3] = 0;
				}
				_driver.SetVariable(doorName + ".Comm", bytes.ToLong(), opDesc);
				return;
			case 67:
				if (bytes.Length > 3)
				{
					bytes[3] = 1;
				}
				_driver.SetVariable(doorName + ".Comm", bytes.ToLong(), opDesc);
				return;
			}
			Trace.Warning($"Can not update {doorName} comm or status [added to .Event] - EventId not known > opDesc {opDesc} > eventID={obj.EventID}, SubjId={obj.SubjectID}, doorId={obj.DoorID}.", "UpdateOnlineDoor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 595);
			List<NameValue> list = new List<NameValue>
			{
				NameValue.op_Implicit($"EventId={obj.EventID}"),
				NameValue.op_Implicit("DoorId=" + obj.DoorID),
				NameValue.op_Implicit($"DateUtc={obj.DateUtc}"),
				NameValue.op_Implicit("SubjectId=" + obj.SubjectID),
				NameValue.op_Implicit("OpDesc=" + opDesc)
			};
			_driver.SetVariableNoCacheWithParameters(doorName + ".Event", obj.EventID, opDesc, list.ToArray());
		}
		catch (Exception ex)
		{
			Trace.Debug("Channel_EventDoorReceived => Then exception for online door-> extid={obj?.DoorID} / ev={obj?.EventID}", "UpdateOnlineDoor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 657);
			Trace.Debug(ex.Message, "UpdateOnlineDoor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 658);
		}
	}

	private void Channel_HookEventReceived(HookEvent e)
	{
		Trace.Debug($"Device_HookReceived > {e}", "Channel_HookEventReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 664);
		try
		{
			switch (e.HookEventType)
			{
			case HookEventType.Relay_On:
			case HookEventType.Relay_Off:
				HookEvent_OutputReceived(e);
				break;
			case HookEventType.Com_lost:
				HookEvent_ComLost(e);
				break;
			}
			int operationId = e.OperationId;
			if ((uint)(operationId - 1) <= 1u)
			{
				HookEvent_InputReceived(e);
			}
			if (!string.IsNullOrWhiteSpace(e?.DoorExtId))
			{
				HookEvent_GenericDoorEvent(e);
			}
			else
			{
				HookEvent_GenericTrace(e);
			}
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "Channel_HookEventReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 703);
		}
	}

	private void HookEvent_GenericTrace(HookEvent e)
	{
		_driver.SetVariableNoCacheWithParameters(((TreeRow)_rootVar).Name + ".Trace", e.OperationId, e.OperationName, e.ToNameValue().ToArray());
	}

	private void HookEvent_GenericDoorEvent(HookEvent e)
	{
		VariableRow val = _rows.FirstOrDefault((VariableRow x) => x.Address == e.DoorExtId);
		if (val == null)
		{
			Trace.Debug("Can not find row with address=" + e.DoorExtId, "HookEvent_GenericDoorEvent", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 717);
		}
		else
		{
			_driver.SetVariableNoCacheWithParameters(((TreeRow)val).Name + ".Event", e.OperationId, e.OperationName, e.ToNameValue().ToArray());
		}
	}

	private void HookEvent_ComLost(HookEvent e)
	{
		if (e.OperationId < 1000)
		{
			VariableRow val = _rows.FirstOrDefault((VariableRow x) => x.Address == e.DoorExtId);
			Trace.Error("<dvr< AddAlarm: " + e.DoorName + ": " + e.OperationName, "HookEvent_ComLost", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 732);
			((AppServer)_driver.AppServer).AlarmManager.AddAlarmExt(50, "Communication lost", ((val != null) ? ((TreeRow)val).FullDescription : null) ?? e.DoorName, e.OperationName, "", "", Array.Empty<NameValue>());
		}
	}

	private void HookEvent_InputReceived(HookEvent e)
	{
		if (_driver.IsSaltoOEM)
		{
			VariableRow val = _rows.FirstOrDefault((VariableRow x) => Helper.GetParameter("InputName", x.Parameters, (string)null) == e.InputName);
			if (val == null)
			{
				Trace.Debug("Can not find row with InputName=" + e.InputName, "HookEvent_InputReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 744);
				return;
			}
			bool flag = e.OperationId == 1;
			_driver.SetVariableNoCacheWithParameters(((TreeRow)val).Name, flag, e.OperationName, e.ToNameValue().ToArray());
		}
	}

	private void HookEvent_OutputReceived(HookEvent e)
	{
		if (_driver.IsSaltoOEM)
		{
			VariableRow val = _rows.FirstOrDefault((VariableRow x) => Helper.GetParameter("RelayName", x.Parameters, (string)null) == e.RelayName);
			if (val == null)
			{
				Trace.Debug("Can not find row with RelayName=" + e.RelayName, "HookEvent_OutputReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 758);
				return;
			}
			bool flag = e.OperationId == 5 || e.OperationId == 7;
			_driver.SetVariableNoCacheWithParameters(((TreeRow)val).Name, flag, e.OperationName, e.ToNameValue().ToArray());
		}
	}

	private void ChannelOnEventOnlineDoor(OnlineDoorStatus obj)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		if ((int)Comm != 4)
		{
			Comm = (CommStatus)1;
		}
		if (string.IsNullOrWhiteSpace(obj?.DoorID))
		{
			return;
		}
		string text = "";
		try
		{
			text = (_useExtIds ? GetDoorRowFullNameFromDoorExtId(obj?.DoorID) : GetDoorRowFullNameFromDoorId(obj?.DoorID));
			if (string.IsNullOrWhiteSpace(text))
			{
				VariableRow obj2 = _rows.FirstOrDefault((VariableRow x) => x.Address == obj.DoorID);
				text = ((obj2 != null) ? ((TreeRow)obj2).Name : null);
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				if (_logLimiter.TryAdd("ChannelOnEventOnlineDoor " + obj.DoorID, DateTime.Now))
				{
					Trace.Debug("ChannelOnEventOnlineDoor - Can not find doorName for " + obj.DoorID + " / " + obj.DoorTypeDesc, "ChannelOnEventOnlineDoor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 785);
				}
				return;
			}
			if (_doorsStatus.ContainsKey(obj.DoorID))
			{
				_doorsStatus[obj.DoorID].IsOnline = true;
			}
			else
			{
				string text2 = $"doorStatus not followed for {obj.DoorID} > {text} of {_doorsStatus.Count} followed > byname:{_doorsStatus.ContainsKey(text)} > useExtId: {_useExtIds} expected: {_doorsStatus.FirstOrDefault().Key} for row:{_doorsStatus.FirstOrDefault().Value?.RowName}";
				if (!_logLimiter.ContainsKey(text2))
				{
					Trace.Debug(text2, "ChannelOnEventOnlineDoor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 799);
					_logLimiter.TryAdd(text2, DateTime.Now);
				}
			}
			_lastOnlineDoorUpdate[obj.DoorID] = DateTime.Now;
			_driver.SetVariable(text + ".Status", obj.DoorStatus + 1, obj.DoorStatusDesc);
			bool flag = obj.BatteryStatus == -1 || obj.TamperStatus == -1 || obj.CommStatus == -1;
			bool flag2 = obj.CommStatus == 0;
			bool flag3 = obj.BatteryStatus > 0;
			bool flag4 = obj.TamperStatus > 0;
			bool[] source = new bool[4] { flag, flag2, flag3, flag4 };
			_driver.SetVariable(text + ".Comm", source.ToLong());
		}
		catch (Exception ex)
		{
			Trace.Error($"Can not update status of {text} with {obj} > {_doorsStatus?.ContainsKey(obj.DoorID)}/{_lastOnlineDoorUpdate?.ContainsKey(obj.DoorID)} > {ex}", "ChannelOnEventOnlineDoor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 823);
		}
	}

	private void Channel_EventStreamReceived(StreamEvent e)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Expected I4, but got Unknown
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Expected I4, but got Unknown
		if ((int)Comm != 4)
		{
			Comm = (CommStatus)1;
		}
		switch (e.UserType)
		{
		case 0:
		{
			Trace.Debug($"Event stream type: cardholder user={e.UserName} / op={e.OperationID} / cardId={e.UserCardID} / sn={e.UserCardSerialNumber} / desc={SaltoSdkHelper.GetOperationDescription(e.OperationID)}", "Channel_EventStreamReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 843);
			string text = (_useExtIds ? GetDoorRowFullNameFromDoorExtId(e.DoorExtID) : GetDoorRowFullNameFromDoorId(e.DoorName));
			if (string.IsNullOrWhiteSpace(text))
			{
				VariableRow obj = _rows.FirstOrDefault((VariableRow x) => x.Address == e.DoorExtID || x.Address == e.DoorName);
				text = ((obj != null) ? ((TreeRow)obj).Name : null);
			}
			if (string.IsNullOrEmpty(text))
			{
				Trace.Debug("Channel_EventStreamReceived) - Door unknown: " + e.DoorExtID + " / name=" + e.DoorName, "Channel_EventStreamReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 853);
				break;
			}
			Person personByBadgeCode = PrysmSdkHelper.GetPersonByBadgeCode(e.UserName);
			if (personByBadgeCode != null)
			{
				Badge val = ((IEnumerable<Badge>)personByBadgeCode.Badges)?.FirstOrDefault((Badge x) => x.Code == e.UserName);
				if (val != null && val.CustomExtension != e.UserCardSerialNumber)
				{
					PrysmSdkHelper.StoreRomCode(personByBadgeCode.Id, e.UserName, e.UserCardSerialNumber);
				}
				break;
			}
			try
			{
				string info;
				ReaderStatus readerStatus = SaltoSdkHelper.GetReaderStatus(e.OperationID, out info);
				text += ".Reader";
				Trace.Debug($">>> update reader from eventStream > enum > {text}={(int)readerStatus} - {e.OperationDescription} ({e.UserName}) - eventDateTime={e.EventDateTimeUTC}, eventDate={e.EventTimeUTC}", "Channel_EventStreamReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 894);
				_driver.SetVariableNoCacheWithParameters(text, (int)readerStatus, e.OperationDescription + " (" + e.UserName + ")", e.ToNameValue().ToArray());
				break;
			}
			catch (Exception ex)
			{
				Trace.Error(ex, null, "Channel_EventStreamReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 899);
				break;
			}
		}
		case 1:
			Trace.Debug($"Event stream type: Door {e.UserName} / {e.OperationID} / {e.UserCardID} / {e.OperationDescription} / {e.DoorExtID} = {e.DoorName}  >> not updated from here (use SHIP - most complete)", "Channel_EventStreamReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 905);
			break;
		case 2:
			Trace.Debug($"Event stream type: software operator {e.UserName} / {e.OperationID} = {e.OperationDescription} / {e.UserCardID}  >> not updated from here (use SHIP - most complete)", "Channel_EventStreamReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 949);
			break;
		default:
			Trace.Error("Event type unknown: " + e, "Channel_EventStreamReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 953);
			break;
		}
	}

	private void Channel_EventCardReceived(EventCard obj)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Expected O, but got Unknown
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_034c: Expected O, but got Unknown
		//IL_036d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0377: Expected O, but got Unknown
		//IL_0389: Unknown result type (might be due to invalid IL or missing references)
		//IL_0393: Expected O, but got Unknown
		//IL_03aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Expected O, but got Unknown
		//IL_03cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d5: Expected O, but got Unknown
		//IL_03ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f6: Expected O, but got Unknown
		//IL_0456: Unknown result type (might be due to invalid IL or missing references)
		//IL_0423: Unknown result type (might be due to invalid IL or missing references)
		//IL_0429: Expected I4, but got Unknown
		//IL_047f: Unknown result type (might be due to invalid IL or missing references)
		if ((int)Comm != 4)
		{
			Comm = (CommStatus)1;
		}
		if (string.IsNullOrWhiteSpace(obj?.DoorID))
		{
			return;
		}
		string doorName = "";
		try
		{
			if (_useExtIds)
			{
				doorName = GetDoorRowFullNameFromDoorExtId(obj?.DoorID);
			}
			else
			{
				doorName = GetDoorRowFullNameFromDoorId(obj?.DoorID);
			}
			if (string.IsNullOrWhiteSpace(doorName))
			{
				VariableRow obj2 = _rows.FirstOrDefault((VariableRow x) => x.Address == obj?.DoorID);
				doorName = ((obj2 != null) ? ((TreeRow)obj2).Name : null);
			}
			if (string.IsNullOrEmpty(doorName))
			{
				Trace.Debug("Channel_EventCardReceived - Door unknown: " + obj?.DoorID, "Channel_EventCardReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 979);
				return;
			}
			int num = -1;
			Person val = null;
			if (!string.IsNullOrWhiteSpace(obj?.SubjectID) && !_areUsersInSalto)
			{
				try
				{
					num = PrysmSdkHelper.GetPersonByBadgeCode(obj.SubjectID).Id;
				}
				catch (Exception arg)
				{
					Trace.Warning($"Channel_EventCardReceived exception GetPersonIdByBadge with parameters {obj?.SubjectID} >>> {arg}", "Channel_EventCardReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 993);
					return;
				}
				if (num < 1)
				{
					Trace.Debug($"Channel_EventCardReceived > User unknown: evtID {obj.EventID} / subj {obj.SubjectID} / op {obj.Operation} / {SaltoSdkHelper.GetOperationDescription(obj.Operation)}", "Channel_EventCardReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 998);
				}
				val = _cache.GetPersonFromCache(num);
			}
			bool num2 = num > 0 && val != null;
			string info;
			ReaderStatus readerStatus = SaltoSdkHelper.GetReaderStatus(obj.Operation, out info);
			if (!num2)
			{
				Trace.Debug($"Channel_EventCardReceived> Unknown user on door {doorName} > op={obj.Operation} subject={obj.SubjectID} status={readerStatus}", "Channel_EventCardReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1009);
			}
			doorName += ".Reader";
			Trace.Debug($"<<drv<< update reader SendBadge from ship {doorName}, badge: {obj.SubjectID}, {readerStatus} / userId: {num} / person={((val != null) ? val.FirstName : null)} {((val != null) ? val.LastName : null)} / op={SaltoSdkHelper.GetOperationDescription(obj.Operation)} / {obj.DateUtc}", "Channel_EventCardReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1012);
			List<NameValue> list = new List<NameValue>();
			list.Add(new NameValue("OperationDesc", (object)info));
			list.Add(new NameValue("DoorExtID", (object)obj.DoorID));
			list.Add(new NameValue("EventDateTimeUTC", (object)$"{obj.DateUtc}"));
			list.Add(new NameValue("SubjectID", (object)obj.SubjectID));
			list.Add(new NameValue("OperationInfo", (object)obj.Operation));
			list.Add(new NameValue("EventID", (object)obj.EventID));
			list.Add(new NameValue("Date", (object)obj.DateUtc));
			if (_areUsersInSalto)
			{
				Trace.Debug("Channel_EventCardReceived> SHIP Users in salto", "Channel_EventCardReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1025);
				_driver.SetVariableNoCacheWithParameters(doorName, (int)readerStatus, $"{obj.Operation}>{info}", list.ToArray());
				return;
			}
			if ((int)readerStatus != 0)
			{
				((AppServer)_driver.AppServer).VariableManager.SetBadge(doorName, obj.SubjectID, "", readerStatus, num, obj.DateUtc);
				return;
			}
			VariableRow val2 = _rows.FirstOrDefault((VariableRow x) => ((TreeRow)x).Name == doorName);
			object obj3;
			if (val2 == null)
			{
				obj3 = null;
			}
			else
			{
				string[] groupNames = val2.GroupNames;
				obj3 = ((groupNames != null) ? CollectionExtensions.Join<string>((IEnumerable<string>)groupNames, ",") : null);
			}
			if (obj3 == null)
			{
				obj3 = "";
			}
			string text = (string)obj3;
			((AppServer)_driver.AppServer).EventManager.AddEventExt("Badge Event", info, doorName, (val2 != null) ? val2.AreaName : null, text, list.ToArray());
		}
		catch (Exception arg2)
		{
			Trace.Error($"Device.Channel_EventCardReceived exception address={obj?.DoorID} => {doorName}    >>> {arg2}", "Channel_EventCardReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1061);
		}
	}

	private void Channel_ConnectionEventReceived(ConnectionStatus obj)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected I4, but got Unknown
		_driver.SetVariable(((TreeRow)_rootVar).Name + ".Comm", (int)obj.NetworkConnection, obj.Message);
	}

	private void CheckOnlineDoorLastUpdate()
	{
	}

	private void CancelSaltoKey(string sourceId)
	{
		_channel.CancelSaltoKey(sourceId.ToString());
	}

	public string PassMessage(string keyId)
	{
		Trace.Debug("PassMessage for " + keyId, "PassMessage", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1224);
		if (string.IsNullOrWhiteSpace(keyId))
		{
			return "2";
		}
		string text = "";
		if (keyId.StartsWith("[NEWKEY]", StringComparison.InvariantCulture))
		{
			return GetResponseKeyDataForNewKey(keyId);
		}
		return GetResponseKeyData(keyId);
	}

	public string GetResponseKeyDataForNewKey(string newKey)
	{
		try
		{
			string rOMCode = newKey.Replace("[NEWKEY]", "");
			int personIdByBadge = PrysmSdkHelper.Datalayer.GetPersonIdByBadge(newKey, "", 1);
			_ = PrysmSdkHelper.Datalayer;
			if (_readerNames == null || !_readerNames.Any())
			{
				return "2";
			}
			UserRights userRightsFromCache = _cache.GetUserRightsFromCache(personIdByBadge);
			if (userRightsFromCache == null)
			{
				return "2";
			}
			ResponseKeyData responseKeyData = SaltoResponse.GetResponseKeyData(userRightsFromCache, personIdByBadge.ToString(), _maxValidityInHours);
			return DataSerializer<object>.Serializer(new ResponseKeyDataForNewKey
			{
				ROMCode = rOMCode,
				KeyID = personIdByBadge.ToString(),
				SaltoKeyData = responseKeyData.SaltoKeyData
			});
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "GetResponseKeyDataForNewKey", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1281);
			return ex.Message;
		}
	}

	public string GetResponseKeyData(string keyId)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(keyId))
			{
				return "2";
			}
			int num = -1;
			try
			{
				num = PrysmSdkHelper.Datalayer.GetPersonIdByBadge(keyId, "", 1);
			}
			catch (Exception arg)
			{
				Trace.Error($"{((TreeRow)_rootVar).Name} - GetResponseKeyData GetPersonIdByBadge exception for {keyId}: {arg}", "GetResponseKeyData", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1301);
			}
			if (num < 1)
			{
				Trace.Debug(((TreeRow)_rootVar).Name + " - GetResponseKeyData Unknown user for keyId = " + keyId, "GetResponseKeyData", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1306);
				return "2";
			}
			if (_readerNames == null || !_readerNames.Any())
			{
				Trace.Debug(((TreeRow)_rootVar).Name + " - GetResponseKeyData - No reader names", "GetResponseKeyData", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1312);
				return "2";
			}
			UserRights userRightsFromCache = _cache.GetUserRightsFromCache(num);
			if (userRightsFromCache == null)
			{
				Trace.Debug($"{((TreeRow)_rootVar).Name} - GetResponseKeyData - No userrights found for personId={num} ({keyId})", "GetResponseKeyData", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1325);
				return "2";
			}
			if (userRightsFromCache.Person == null || userRightsFromCache.Person.Id < 1)
			{
				Trace.Debug($"{((TreeRow)_rootVar).Name} - GetResponseKeyData - No person in userRights personId={num} ({keyId})", "GetResponseKeyData", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1330);
				return "2";
			}
			ResponseKeyData responseKeyData = SaltoResponse.GetResponseKeyData(userRightsFromCache, keyId, _maxValidityInHours);
			if (responseKeyData?.SaltoKeyData == null)
			{
				Trace.Debug($"{((TreeRow)_rootVar).Name} - GetResponseKeyData - No response saltoKeyData personId={num} ({keyId})", "GetResponseKeyData", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1338);
				return "2";
			}
			return DataSerializer<object>.Serializer(responseKeyData);
		}
		catch (Exception ex)
		{
			Trace.Error($"Can not GetResponseKeyData for {keyId} >>> {ex}", "GetResponseKeyData", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1349);
			return ex.Message;
		}
	}

	private void GetLastAuditTrailId()
	{
		try
		{
			VariableState stateByName = ((IRowStateManager<VariableRow, VariableState>)(object)((AppServer)_appServer).VariableManager).GetStateByName(((TreeRow)_rootVar).Name + ".LastAuditTrailId");
			_lastAuditTrailId = (int)stateByName.Value;
		}
		catch (Exception ex)
		{
			Trace.Warning("Fail to retrieve LastAuditTrailId: " + ex.Message, "GetLastAuditTrailId", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1368);
		}
	}

	private void SetLastAuditTrailId(int eventId)
	{
		_lastAuditTrailId = eventId;
		try
		{
			((AppServer)_appServer).VariableManager.Set(((TreeRow)_rootVar).Name + ".LastAuditTrailId", (object)eventId, (string)null, (string)null, default(DateTimeOffset), 0, 0);
		}
		catch (Exception ex)
		{
			Trace.Warning("Fail to set LastAuditTrailId: " + ex.Message, "SetLastAuditTrailId", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1381);
		}
	}

	private string GetDoorRowFullNameFromDoorId(string doorID)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(doorID))
			{
				return "";
			}
			if (_doorsRows == null)
			{
				InitializeAllDoors();
			}
			VariableRow[] doorsRows = _doorsRows;
			object obj;
			if (doorsRows == null)
			{
				obj = null;
			}
			else
			{
				VariableRow obj2 = doorsRows.FirstOrDefault((VariableRow x) => ((x == null) ? null : x.Address?.ToLower()?.Trim()) == doorID?.ToLower()?.Trim());
				obj = ((obj2 != null) ? ((TreeRow)obj2).Name : null);
			}
			string text = (string)obj;
			try
			{
				if (string.IsNullOrWhiteSpace(text) && _lastFoundWarning.TryGetValue(doorID, out var value) && value.AddHours(3.0) < DateTime.Now)
				{
					_lastFoundWarning[doorID] = DateTime.Now;
					Trace.Info("DoorID '" + doorID + "' not found in Address field. Check config to not loose infos about this door", "GetDoorRowFullNameFromDoorId", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1632);
				}
			}
			catch (Exception arg)
			{
				Trace.Debug($"ex with {text} > {arg}", "GetDoorRowFullNameFromDoorId", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1637);
			}
			return text;
		}
		catch (Exception ex)
		{
			object[] obj3 = new object[5]
			{
				_doorsRows?.Count(),
				null,
				null,
				null,
				null
			};
			VariableRow[] doorsRows2 = _doorsRows;
			object obj4;
			if (doorsRows2 == null)
			{
				obj4 = null;
			}
			else
			{
				VariableRow obj5 = doorsRows2.FirstOrDefault();
				obj4 = ((obj5 != null) ? ((TreeRow)obj5).Name : null);
			}
			obj3[1] = obj4;
			VariableRow[] doorsRows3 = _doorsRows;
			object obj6;
			if (doorsRows3 == null)
			{
				obj6 = null;
			}
			else
			{
				VariableRow obj7 = doorsRows3.FirstOrDefault();
				obj6 = ((obj7 != null) ? obj7.Address : null);
			}
			obj3[2] = obj6;
			obj3[3] = doorID;
			obj3[4] = ex;
			Trace.Error(string.Format("GetDoorRowFullNameFromDoorId: rows={0} first row: {1} / address:{2} (search {3})  >>>> {4}", obj3), "GetDoorRowFullNameFromDoorId", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1644);
			return "";
		}
	}

	private string GetDoorRowFullNameFromDoorExtId(string doorExtId)
	{
		if (doorExtId == null)
		{
			return "";
		}
		try
		{
			if (_doorsRows == null)
			{
				InitializeAllDoors();
			}
			VariableRow[] doorsRows = _doorsRows;
			object obj;
			if (doorsRows == null)
			{
				obj = null;
			}
			else
			{
				VariableRow obj2 = doorsRows.FirstOrDefault((VariableRow x) => ((x != null) ? x.Address : null) == doorExtId);
				obj = ((obj2 != null) ? ((TreeRow)obj2).Name : null);
			}
			string result = (string)obj;
			try
			{
				if (string.IsNullOrWhiteSpace(doorExtId) && _lastFoundWarning.TryGetValue(doorExtId, out var value) && value.AddHours(3.0) < DateTime.Now)
				{
					_lastFoundWarning[doorExtId] = DateTime.Now;
					Trace.Info("ExtId '" + doorExtId + "' not found in Address field. Check config to not loose infos about this door", "GetDoorRowFullNameFromDoorExtId", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1678);
				}
			}
			catch (Exception arg)
			{
				Trace.Debug($"ex with {doorExtId} > {arg}", "GetDoorRowFullNameFromDoorExtId", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1683);
			}
			return result;
		}
		catch (Exception ex)
		{
			object[] obj3 = new object[4]
			{
				_doorsRows?.Count(),
				null,
				null,
				null
			};
			VariableRow[] doorsRows2 = _doorsRows;
			object obj4;
			if (doorsRows2 == null)
			{
				obj4 = null;
			}
			else
			{
				VariableRow obj5 = doorsRows2.FirstOrDefault();
				obj4 = ((obj5 != null) ? ((TreeRow)obj5).Name : null);
			}
			obj3[1] = obj4;
			VariableRow[] doorsRows3 = _doorsRows;
			object obj6;
			if (doorsRows3 == null)
			{
				obj6 = null;
			}
			else
			{
				VariableRow obj7 = doorsRows3.FirstOrDefault();
				obj6 = ((obj7 != null) ? obj7.Address : null);
			}
			obj3[2] = obj6;
			obj3[3] = doorExtId;
			Trace.Error(string.Format("GetDoorRowFullNameFromDoorExtId exception (Check config!). rows={0} first row: {1} / address:{2} (search {3})", obj3), "GetDoorRowFullNameFromDoorExtId", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1690);
			Trace.Debug(ex.ToString(), "GetDoorRowFullNameFromDoorExtId", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1691);
			return "";
		}
	}

	private int GetUserId(string subjectID)
	{
		if (int.TryParse(subjectID, out var result))
		{
			return result;
		}
		return 0;
	}

	private async Task InitializeAllDoors()
	{
		VariableRow rootVar = _rootVar;
		Trace.Debug("Initialize registered states of all doors in: " + ((rootVar != null) ? ((TreeRow)rootVar).Name : null) + ".*", "InitializeAllDoors", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1707);
		await Task.Run(delegate
		{
			try
			{
				CollectionExtensions.ReplaceAll<VariableRow>((ICollection<VariableRow>)_rows, (IEnumerable<VariableRow>)((IRowStateManager<VariableRow, VariableState>)(object)((AppServer)_driver.AppServer).VariableManager).GetRowsByName(((TreeRow)_rootVar).Name + ".*"));
				_doorsRows = (from a in ((IRowStateManager<VariableRow, VariableState>)(object)((AppServer)_appServer).VariableManager).GetRowsByName(((TreeRow)_rootVar).Name + ".*")
					where (int)a.Type == 0
					select a into x
					where !string.IsNullOrWhiteSpace(x.Address)
					select x).ToArray();
				VariableRow[] source = (from a in ((IRowStateManager<VariableRow, VariableState>)(object)((AppServer)_appServer).VariableManager).GetRowsByName(((TreeRow)_rootVar).Name + ".*")
					where (int)a.Type == 10
					select a).ToArray();
				_readerNames = (from x in source
					where ((TreeRow)x).Name.ToLower().EndsWith("reader")
					select ((TreeRow)x).Name).ToArray();
				_relays = (from x in source
					where ((TreeRow)x).Name.ToLower().Contains("relay")
					select ((TreeRow)x).Name).ToArray();
				Trace.Debug($"Found {_doorsRows.Length} doors / {_readerNames.Length} readers / {_relays} relays", "InitializeAllDoors", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1718);
				if (_doorsRows.Length < 1)
				{
					Trace.Warning("WARNING: No door found", "InitializeAllDoors", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1721);
				}
				Parallel.ForEach(_doorsRows, InitializeDoorRow);
				Trace.Debug($"All {_doorsRows?.Length} doors initialized.", "InitializeAllDoors", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1724);
			}
			catch (Exception ex)
			{
				Trace.Error($"Initialize all doors failed: {ex.Message} > {ex}", "InitializeAllDoors", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1728);
			}
		});
	}

	private void InitializeDoorRow(VariableRow row)
	{
		if (_doorsStatus.ContainsKey(row.FullAddress))
		{
			return;
		}
		try
		{
			Trace.Debug("Initialize door status " + ((TreeRow)row).Name + " (" + row.FullAddress + ")", "InitializeDoorRow", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1742);
			Door door = new Door(row.FullAddress, ((TreeRow)row).Name);
			if ((Helper.GetParameter("Type", row.FullParameters, (string)null) ?? Helper.GetParameter("DoorType", row.FullParameters, (string)null))?.ToLower() == "zones")
			{
				door.IsZone = true;
			}
			else
			{
				door.IsZone = false;
				InitializeOfflineDoorStatus(row, door);
				InitializeOnlineDoorStatus(row, door);
				InitializeOnlineDoorComm(row, door);
			}
			Trace.Debug(string.Format("Store in _doorsStatus: {0} at {1} is {2} - is online: {3} /status: {4} / online status: {5} / online status 2: {6} / low battery: {7}", ((TreeRow)row).Name, row.FullAddress, door.IsZone ? "zone" : "door", door.IsOnline, door.Status, door.OnlineStatus, door.OnlineStatus2, door.LowBattery), "InitializeDoorRow", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1757);
			if (!_doorsStatus.TryAdd(row.FullAddress, door))
			{
				Trace.Error("Can not add " + ((TreeRow)row).Name + " (" + row.FullAddress + ") in _doorsStatus", "InitializeDoorRow", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1760);
			}
			else
			{
				Trace.Debug($"STORED {((TreeRow)row).Name} ({row.FullAddress}) in _doorsStatus: {_doorsStatus.ContainsKey(row.FullAddress)} > Total: {_doorsStatus?.Count()}", "InitializeDoorRow", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1762);
			}
		}
		catch (Exception ex)
		{
			Trace.Error("ERROR ! Door not Initialized => " + ((TreeRow)row).Name + " (" + row.FullAddress + "): " + ex.Message, "InitializeDoorRow", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1766);
		}
	}

	private void InitializeOnlineDoorComm(VariableRow row, Door newDoor)
	{
		try
		{
			if (_rows.Any((VariableRow x) => ((TreeRow)x).Name == ((TreeRow)row).Name + ".Comm" && Helper.GetParameter<bool>("IsOnline", row.FullParameters, false)))
			{
				VariableState stateByName = ((IRowStateManager<VariableRow, VariableState>)(object)((AppServer)_appServer).VariableManager).GetStateByName(((TreeRow)row).Name + ".Comm");
				if (stateByName != null && stateByName.Value != null)
				{
					newDoor.IsOnline = true;
					newDoor.OnlineStatus2 = (int)stateByName.Value;
				}
			}
		}
		catch (Exception ex)
		{
			Trace.Debug("InitializeDoorOnlineStatus2 (comm) Exception " + ((TreeRow)row).Name, "InitializeOnlineDoorComm", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1786);
			Trace.Error(ex, null, "InitializeOnlineDoorComm", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1787);
		}
	}

	private void InitializeOnlineDoorStatus(VariableRow row, Door newDoor)
	{
		try
		{
			if (_rows.Any((VariableRow x) => ((TreeRow)x).Name == ((TreeRow)row).Name + ".Status" && Helper.GetParameter<bool>("IsOnline", row.FullParameters, false)))
			{
				VariableState stateByName = ((IRowStateManager<VariableRow, VariableState>)(object)((AppServer)_appServer).VariableManager).GetStateByName(((TreeRow)row).Name + ".Status");
				if (stateByName != null && stateByName.Value != null)
				{
					newDoor.IsOnline = true;
					newDoor.OnlineStatus = (int)stateByName.Value;
				}
			}
		}
		catch (Exception ex)
		{
			Trace.Debug("InitializeDoorOnlineStatus Exception " + ((TreeRow)row).Name, "InitializeOnlineDoorStatus", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1807);
			Trace.Error(ex, null, "InitializeOnlineDoorStatus", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1808);
		}
	}

	private void InitializeOfflineDoorStatus(VariableRow row, Door newDoor)
	{
		try
		{
			if (_rows.Any((VariableRow x) => ((TreeRow)x).Name == ((TreeRow)row).Name + ".Status" && !Helper.GetParameter<bool>("IsOnline", row.FullParameters, false)))
			{
				VariableState stateByName = ((IRowStateManager<VariableRow, VariableState>)(object)((AppServer)_appServer).VariableManager).GetStateByName(((TreeRow)row).Name + ".Status");
				if (stateByName != null && stateByName.Value != null)
				{
					int status = (int)stateByName.Value;
					newDoor.Status = status;
				}
			}
		}
		catch (Exception ex)
		{
			Trace.Debug("InitializeOfflineDoorStatus Exception " + ((TreeRow)row).Name, "InitializeOfflineDoorStatus", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1830);
			Trace.Error(ex, null, "InitializeOfflineDoorStatus", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1831);
		}
	}

	internal void UpdateModifications(List<ModifRow> modifs)
	{
		foreach (ModifRow modif in modifs)
		{
			if (!(modif.Table.ToLower() != "apc_badges"))
			{
				switch (modif.Type)
				{
				case 2:
					Trace.Debug($"{((TreeRow)_rootVar).Name} Cancel Key : {modif.Description} - userId={modif.SourceId}", "UpdateModifications", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1851);
					CancelSaltoKey(modif.Description);
					continue;
				case 0:
					Trace.Debug($"{((TreeRow)_rootVar).Name} Add Key : {modif.Description} - userId={modif.SourceId}", "UpdateModifications", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1857);
					continue;
				case 1:
					continue;
				}
				Trace.Debug($"{((TreeRow)_rootVar).Name} Device.UpdateModifications row : {modif.Description} - info: {modif.Info} - type: {modif.Type} / src1 {modif.SourceId} / userId={modif.UserId}", "UpdateModifications", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1860);
			}
		}
	}

	internal void UpdateModifications(CommandDownloadList cmd)
	{
		PrysmSdkHelper.ProtocolName = _appServer.CurrentProtocol.Name;
		PrysmSdkHelper.Driver = _driver;
		_ = cmd.Holidays;
		_ = cmd.Parameters;
		if (cmd.Timetables.Count > 0)
		{
			UpdateTimetables(cmd);
		}
		if (cmd.Users.Count > 0)
		{
			UpdateUsers(cmd);
		}
	}

	private void UpdateUsers(CommandDownloadList cmd)
	{
		Trace.Debug($"UpdateModifications {cmd.Users.Count} users", "UpdateUsers", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1892);
		Task.Run(async delegate
		{
			try
			{
				Comm = (CommStatus)4;
				List<UserRights> obj = await PrysmSdkHelper.GetPeopleUserRights(_readerNames, cmd.Users.ToArray(), _relays);
				int num = 0;
				foreach (UserRights item in obj)
				{
					if (!_cache.SaveUserRightsInCache(item))
					{
						num++;
					}
				}
				if (num > 0)
				{
					_driver.AddAlarm(new Exception($"ERROR !! {num} found updating USERS"));
				}
			}
			catch (Exception arg)
			{
				_driver.AddAlarm(new Exception($"EXCEPTION !! found updating USERS  >> {arg}"));
			}
			finally
			{
				Comm = (CommStatus)1;
			}
		});
	}

	private void UpdateTimetables(CommandDownloadList cmdList)
	{
		Task.Run(delegate
		{
			Comm = (CommStatus)4;
			try
			{
				int countErrors = 0;
				Trace.Debug($"UpdateModifications {cmdList.Timetables.Count} timetable(s)", "UpdateTimetables", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1930);
				Parallel.ForEach(cmdList.Timetables, delegate(int timetableId)
				{
					if (!UpdateTimetable(timetableId))
					{
						countErrors++;
					}
				});
				if (countErrors > 0)
				{
					_driver.AddAlarm(new Exception($"ERROR !! {countErrors} found updating Timetables"));
				}
			}
			finally
			{
				Comm = (CommStatus)1;
			}
		});
	}

	private bool UpdateTimetable(int timetableId)
	{
		try
		{
			PrysmSdkHelper.ClearTimetables();
			Timetable timetableById = PrysmSdkHelper.Datalayer.GetTimetableById(timetableId);
			_cache.SaveTimetableInCache(timetableById, timetableId);
			Person[] people = PrysmSdkHelper.Datalayer.GetPeople();
			List<int> users = (from x in PrysmSdkHelper.GetPeopleUserRights(_readerNames, people.Select((Person x) => x.Id).ToArray(), _relays).Result
				where x.Timetables.Any((UserTimetable y) => y.TimetableId == timetableId)
				select x.PersonId).ToList();
			UpdateUsers(new CommandDownloadList
			{
				Users = users
			});
			return true;
		}
		catch (Exception arg)
		{
			Trace.Error($"UpdateModifications error for ttID={timetableId} >>> {arg}", "UpdateTimetable", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1977);
			return false;
		}
	}

	public async Task Refresh(string message)
	{
		Trace.Info("Refresh " + ((TreeRow)_rootVar).Name, "Refresh", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1988);
		Comm = (CommStatus)4;
		await InitializeAllDoors();
		Comm = (CommStatus)1;
	}

	private List<int> BuildTimetableIndexes(PersonMapRight[] mapRights)
	{
		Trace.Debug("Build timetable indexes ...", "BuildTimetableIndexes", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 1995);
		List<int> list = new List<int>();
		foreach (PersonMapRight val in mapRights)
		{
			if (val == null || val.TimetableMap == null)
			{
				continue;
			}
			TimetableAndValidity[] timetableMap = val.TimetableMap;
			foreach (TimetableAndValidity val2 in timetableMap)
			{
				if (val2 != null && val2.TimetableId > 0 && !list.Contains(val2.TimetableId))
				{
					list.Add(val2.TimetableId);
				}
			}
		}
		return list;
	}

	public async Task DriverDownload(string varName, string parameters)
	{
		Trace.Info(((TreeRow)_rootVar).Name + " enter DriverDownload with parameters " + varName + " / " + parameters, "DriverDownload", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 2015);
		int countErrors = 0;
		try
		{
			Trace.Debug("Try GetPeopleMapByReaderNames", "DriverDownload", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 2019);
			Comm = (CommStatus)4;
			PrysmSdkHelper.ProtocolName = _appServer.CurrentProtocol.Name;
			PrysmSdkHelper.Driver = _driver;
			PrysmSdkHelper.ClearTimetables();
			parameters.Split(',');
			List<UserRights> list = await PrysmSdkHelper.GetPeopleUserRights(_readerNames, new int[0], _relays);
			Trace.Info("Write cache", "DriverDownload", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 2033);
			foreach (UserRights item in list)
			{
				if (item?.Person != null)
				{
					_cache.SaveUserRightsInCache(item);
					_cache.SavePersonInCache(item.Person);
				}
			}
			Trace.Info("All userrights and person cache created", "DriverDownload", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 2041);
			foreach (UserTimetable[] item2 in list.Select((UserRights x) => x.Timetables))
			{
				foreach (UserTimetable userTimetable in item2)
				{
					_cache.SaveTimetableInCache(userTimetable.TimeTable, userTimetable.TimetableId);
				}
			}
			Trace.Info("All timetables cache created", "DriverDownload", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 2047);
		}
		catch (Exception ex)
		{
			Trace.Error($"[{((TreeRow)_rootVar).Name}] DriverDownload Exception with parameters {varName} / {parameters} >>> {ex}", "DriverDownload", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 2051);
			_driver.AddAlarm(ex);
		}
		finally
		{
			if ((int)Comm == 4)
			{
				Comm = (CommStatus)1;
			}
			if (countErrors > 0)
			{
				Trace.Error($"{countErrors} not updated", "DriverDownload", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 2061);
				_driver.AddAlarm(new Exception($"DOWNLOAD USERS ERROR : {countErrors} errors. IMPORTANT: Check the driver log"));
			}
		}
	}

	private void Housekeeping(object na)
	{
		Trace.Debug("SaltoServer.HouseKeeping", "Housekeeping", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 2078);
		try
		{
			foreach (KeyValuePair<string, DateTime> item in _logLimiter.Where((KeyValuePair<string, DateTime> _) => _.Value < DateTime.Now.AddDays(-1.0)))
			{
				_logLimiter.TryRemove(item.Key, out var _);
			}
		}
		catch (Exception ex)
		{
			Trace.Error("SaltoServer.HouseKeeping failed: " + ex.Message, "Housekeeping", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\SaltoServer.cs", 2086);
		}
	}
}
