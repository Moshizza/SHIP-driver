using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Apis.Salto.Ship;
using Apis.Salto.Ship.Model;
using AppDriverShip.Bridge;
using AppDriverShip.Model;
using AppDriverShip.Salto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prysm.AppVision.Common;
using Prysm.AppVision.Data;
using Prysm.AppVision.SDK;
using Salto.Salto.Ship.Model;

namespace AppDriverShip;

internal class ChannelService
{
	private int _maxEventByCall;

	private readonly ShipApi _ship;

	private Driver _driver;

	private readonly ClientBridgeListener _clientBridge;

	private readonly string _addressin;

	private readonly string _parameters;

	private CancellationTokenSource _stop = new CancellationTokenSource();

	private readonly int ConnectionRetryIntervalInMs;

	private readonly int MonitorAuditTrailIntervalInMs;

	private readonly int MonitorDoorsIntervalInMs;

	private readonly int MonitorOnlineDoorsIntervalInMs;

	private readonly int MonitorZonesIntervalInMs;

	private int _lastAuditTrailId = -1;

	private bool _areUsersInSalto;

	private string address;

	private int _shipPort;

	private int _eventStreamPort;

	private string _eventStreamHost;

	private string _httpsUsername;

	private string _httpsPassword;

	private string _httpsKey;

	private readonly Dictionary<string, List<string>> _zonesDoors = new Dictionary<string, List<string>>();

	private List<SaltoDBDoor> _doors = new List<SaltoDBDoor>();

	private bool _useExtIds;

	private bool _usersEverywhere;

	private bool _useHttps;

	internal int _nextEventId;

	private System.Timers.Timer _timer = new System.Timers.Timer();

	private string _shipAddress;

	public bool IsSaltoOEM { get; set; }

	private CommStatus _networkConnection { get; set; }

	public event Action<ConnectionStatus> ConnectionEventReceived;

	public event Action<int> LastAuditTrailIdReceived;

	public event Action<EventDoor> EventDoorReceived;

	public event Action<OnlineDoorStatus> EventOnlineDoor;

	public event Action<EventCard> EventCardReceived;

	public event Action<StreamEvent> EventStreamReceived;

	public event Action<EventOperator> EventOperatorReceived;

	public event Action<EventDoorUpdate> EventDoorUpdateReceived;

	public event Action<HookEvent> HookEventReceived;

	internal ChannelService(Driver driver, string addressin, string parameters, int lastAuditTrail)
	{
		if (driver?.AppServer == null)
		{
			throw new ArgumentNullException("Channel service driver can not be null");
		}
		if (string.IsNullOrWhiteSpace(addressin))
		{
			throw new ArgumentNullException("addressin");
		}
		if (string.IsNullOrWhiteSpace(parameters))
		{
			throw new ArgumentNullException("parameters");
		}
		if (!TryParseAddress(addressin, out address, out _shipPort))
		{
			throw new ArgumentException("Address incorrect");
		}
		string parameter = Helper.GetParameter("Bridge", parameters, (string)null);
		if (string.IsNullOrWhiteSpace(parameter))
		{
			throw new ArgumentNullException("Missing parameter: Bridge");
		}
		_eventStreamPort = Helper.GetParameter<int>("EventStreamPort", parameters, 6501);
		_eventStreamHost = Helper.GetParameter("EventStreamHost", parameters, "localhost");
		_driver = driver;
		_lastAuditTrailId = lastAuditTrail;
		_nextEventId = lastAuditTrail;
		_parameters = parameters;
		_addressin = addressin;
		MonitorDoorsIntervalInMs = Helper.GetParameter<int>("MonitorDoorsIntervalInMs", _parameters, 180000);
		MonitorOnlineDoorsIntervalInMs = Helper.GetParameter<int>("MonitorOnlineDoorsIntervalInMs", _parameters, 1000);
		MonitorZonesIntervalInMs = Helper.GetParameter<int>("MonitorZonesIntervalInMs", _parameters, 20000);
		MonitorAuditTrailIntervalInMs = Helper.GetParameter<int>("MonitorAuditTrailIntervalInMs", _parameters, 10000);
		ConnectionRetryIntervalInMs = Helper.GetParameter<int>("ConnectionRetryIntervalInMs", _parameters, 10000);
		_areUsersInSalto = Helper.GetParameter<bool>("UsersInSalto", _parameters, false);
		_usersEverywhere = Helper.GetParameter<bool>("UsersEverywhere", _parameters, false);
		_useExtIds = Helper.GetParameter<bool>("UseExtIds", _parameters, true);
		_maxEventByCall = Helper.GetParameter<int>("MaxNumberOfEventsByRequest", _parameters, 100);
		_useHttps = Helper.GetParameter<bool>("UseHttps", _parameters, false);
		_httpsUsername = Helper.GetParameter("HttpsUsername", _parameters, "");
		_httpsPassword = Helper.GetParameter("HttpsPassword", _parameters, "");
		_httpsKey = Helper.GetParameter("HttpsCustomKey", _parameters, "");
		IsSaltoOEM = ((AppServer)driver.AppServer).GetLicenseInfo("CodesOEM").Contains("568");
		Trace.Debug($"ChannelService ctor for {addressin}\r\n    MonitorDoorsIntervalInMs={MonitorDoorsIntervalInMs}\r\n    MonitorAuditTrailIntervalInMs={MonitorAuditTrailIntervalInMs}\r\n    ConnectionRetryIntervalInMs={ConnectionRetryIntervalInMs}\r\n    UsersInSalto={_areUsersInSalto} and everywhere={_usersEverywhere}\r\n    UseExtIds={_useExtIds}\r\n    LastAuditTrailId={_lastAuditTrailId}", ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 101);
		Trace.Debug($"Channel starts Ship {address}, {_shipPort} ", ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 109);
		_shipAddress = address;
		_ship = new ShipApi(address, _shipPort, _useHttps, _httpsUsername, _httpsPassword, _httpsKey);
		Trace.Debug("Channel starts Bridge " + parameter + ", " + addressin + " ", ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 113);
		_clientBridge = ClientBridgeListener.GetInstance(parameter, _ship, driver, addressin);
	}

	private void ClientBridge_EventReceived(HookEvent hook)
	{
		Trace.Debug("Channel hook event transfer for " + hook?.DoorName, "ClientBridge_EventReceived", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 122);
		EventHelper.InvokeAsync(this.HookEventReceived, hook);
	}

	private void Timer_OnElapsed(object sender, ElapsedEventArgs e)
	{
		TcPing();
	}

	private void TcPing()
	{
		try
		{
			using TcpClient tcpClient = new TcpClient();
			if (!tcpClient.ConnectAsync(_shipAddress, _shipPort).Wait(1000))
			{
				_networkConnection = (CommStatus)2;
				EventHelper.InvokeAsync(this.ConnectionEventReceived, new ConnectionStatus
				{
					NetworkConnection = (CommStatus)2,
					Message = "Channel Disconnected from timer"
				});
			}
			else
			{
				_networkConnection = (CommStatus)1;
				EventHelper.InvokeAsync(this.ConnectionEventReceived, new ConnectionStatus
				{
					NetworkConnection = (CommStatus)1,
					Message = "Channel Connected"
				});
			}
		}
		catch
		{
			_networkConnection = (CommStatus)2;
			EventHelper.InvokeAsync(this.ConnectionEventReceived, new ConnectionStatus
			{
				NetworkConnection = (CommStatus)2,
				Message = "Channel Disconnected from timer"
			});
		}
	}

	internal async Task Start()
	{
		Trace.Debug("Start channel", "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 189);
		_stop = new CancellationTokenSource();
		Trace.Debug("Token Ready", "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 191);
		_clientBridge.EventReceived += ClientBridge_EventReceived;
		_timer.Elapsed += Timer_OnElapsed;
		_timer.Interval = 1000.0;
		Task.Factory.StartNew(() => ReconnectShipLoop(), TaskCreationOptions.LongRunning);
		Trace.Debug("Channel started success", "Start", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 198);
	}

	internal async Task Stop()
	{
		Trace.Debug("Channel stop", "Stop", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 203);
		_stop.Cancel();
		_timer.Stop();
		_timer.Elapsed -= Timer_OnElapsed;
		_clientBridge.Stop();
		_clientBridge.EventReceived -= ClientBridge_EventReceived;
		_networkConnection = (CommStatus)2;
		EventHelper.InvokeAsync(this.ConnectionEventReceived, new ConnectionStatus
		{
			NetworkConnection = (CommStatus)2,
			Message = "Channel Disconnected"
		});
	}

	private async Task ReconnectShipLoop()
	{
		Trace.Debug("Ship loop connection", "ReconnectShipLoop", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 224);
		while (!_stop.IsCancellationRequested)
		{
			try
			{
				_ = 4;
				try
				{
					if ((int)_networkConnection != 1)
					{
						try
						{
							await Open();
						}
						catch (Exception ex)
						{
							Trace.Warning("SHIP Can not open: " + ex.Message, "ReconnectShipLoop", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 238);
							throw;
						}
					}
					if ((int)_networkConnection != 1)
					{
						EventHelper.InvokeAsync(this.ConnectionEventReceived, new ConnectionStatus
						{
							NetworkConnection = _networkConnection,
							Message = "Get info Error > Check connexion and config please"
						});
						await Wait(ConnectionRetryIntervalInMs, _stop);
						continue;
					}
					Trace.Debug($"Start monitoring - Are users in Salto {_areUsersInSalto}", "ReconnectShipLoop", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 253);
					_timer?.Start();
					if (!_usersEverywhere)
					{
						if (!_areUsersInSalto)
						{
							Trace.Debug("Users in appControl, use ship to get events", "ReconnectShipLoop", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 259);
							await Task.WhenAny(MonitorDoors(), MonitorOnlineDoors(), MonitorZones(), MonitorEvents());
						}
						else
						{
							Trace.Debug("Users in Salto Space, use eventstream", "ReconnectShipLoop", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 264);
							await Task.WhenAny(MonitorDoors(), MonitorOnlineDoors(), MonitorZones(), MonitorEventStream());
						}
					}
					else
					{
						Trace.Debug("Users everywhere, monitor all", "ReconnectShipLoop", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 270);
						await Task.WhenAny(MonitorDoors(), MonitorOnlineDoors(), MonitorZones(), MonitorEvents(), MonitorEventStream());
					}
				}
				catch (Exception ex2)
				{
					Trace.Debug($"Connection failed >> {ex2}", "ReconnectShipLoop", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 278);
					_networkConnection = (CommStatus)3;
					EventHelper.InvokeAsync(this.ConnectionEventReceived, new ConnectionStatus
					{
						NetworkConnection = (CommStatus)3,
						Message = "ERROR - " + ex2.Message
					});
				}
			}
			finally
			{
				await Wait(ConnectionRetryIntervalInMs, _stop);
			}
		}
		if ((int)_networkConnection != 3)
		{
			_networkConnection = (CommStatus)2;
			EventHelper.InvokeAsync(this.ConnectionEventReceived, new ConnectionStatus
			{
				NetworkConnection = (CommStatus)2,
				Message = "System stopped"
			});
		}
	}

	private async Task MonitorDoors()
	{
		Trace.Debug("MonitorDoors - start", "MonitorDoors", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 306);
		try
		{
			while (!_stop.IsCancellationRequested)
			{
				try
				{
					List<SaltoDBDoor> list = await GetDoors();
					if (list != null && list.Any())
					{
						UpdateAllDoors(list);
					}
				}
				catch (Exception ex)
				{
					Trace.Error(ex, null, "MonitorDoors", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 324);
				}
				finally
				{
					await Wait(MonitorDoorsIntervalInMs, _stop);
				}
			}
		}
		catch (TaskCanceledException ex2)
		{
			Trace.Debug("MonitorDoor task canceled ex > " + ex2.Message, "MonitorDoors", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 334);
		}
		catch (Exception arg)
		{
			Trace.Error($"MonitorDoor ex > {arg}", "MonitorDoors", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 338);
		}
		Trace.Debug("MonitorDoors - End Monitoring", "MonitorDoors", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 342);
	}

	private async Task MonitorOnlineDoors()
	{
		Trace.Debug("MonitorOnlineDoors - start", "MonitorOnlineDoors", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 346);
		try
		{
			while (!_stop.IsCancellationRequested)
			{
				try
				{
					UpdateOnlineDoors(await GetOnlineDoors());
				}
				catch (Exception ex)
				{
					Trace.Error(ex, null, "MonitorOnlineDoors", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 361);
				}
				finally
				{
					await Wait(MonitorOnlineDoorsIntervalInMs, _stop);
				}
			}
		}
		catch (TaskCanceledException ex2)
		{
			Trace.Debug(ex2.Message, "MonitorOnlineDoors", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 371);
		}
		catch (Exception arg)
		{
			Trace.Error($"Monitor online door ex >> {arg}", "MonitorOnlineDoors", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 375);
		}
		Trace.Debug("MonitorOnlineDoors - End Monitoring", "MonitorOnlineDoors", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 379);
	}

	private async Task MonitorZones()
	{
		Trace.Debug("MonitorZones - start", "MonitorZones", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 383);
		try
		{
			while (!_stop.IsCancellationRequested)
			{
				try
				{
					UpdateAllZones(await GetZones());
				}
				catch (Exception ex)
				{
					Trace.Error(ex, null, "MonitorZones", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 400);
				}
				finally
				{
					await Wait(MonitorZonesIntervalInMs, _stop);
				}
			}
		}
		catch (TaskCanceledException ex2)
		{
			Trace.Debug(ex2.Message, "MonitorZones", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 410);
		}
		catch (Exception arg)
		{
			Trace.Error($"Monitor Zones ex >> {arg}", "MonitorZones", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 414);
		}
		Trace.Debug("MonitorZones - End Monitoring", "MonitorZones", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 418);
	}

	private void UpdateAllZones(List<SaltoDBZone> zones)
	{
		Task.Run(delegate
		{
			_zonesDoors.Clear();
			Parallel.ForEach(zones, delegate(SaltoDBZone zone)
			{
				if (string.IsNullOrWhiteSpace(zone?.ExtZoneID) || zone.Door_Zones == null)
				{
					return;
				}
				if (!_zonesDoors.ContainsKey(zone.ExtZoneID))
				{
					_zonesDoors.Add(zone.ExtZoneID, new List<string>());
				}
				if (_zonesDoors[zone.ExtZoneID] == null)
				{
					_zonesDoors[zone.ExtZoneID] = new List<string>();
				}
				foreach (Door_Zone door_Zone in zone.Door_Zones)
				{
					if (!_zonesDoors[zone.ExtZoneID].Contains(door_Zone.ExtDoorID))
					{
						_zonesDoors[zone.ExtZoneID].Add(door_Zone.ExtDoorID);
					}
				}
			});
		});
	}

	private void UpdateOnlineDoors(List<OnlineDoorStatus> doors)
	{
		if (doors == null)
		{
			return;
		}
		Task.Run(delegate
		{
			foreach (OnlineDoorStatus door in doors)
			{
				if (door != null)
				{
					if (string.IsNullOrWhiteSpace(door?.DoorID))
					{
						Trace.Debug("Door without id " + door.DoorTypeDesc, "UpdateOnlineDoors", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 461);
					}
					else
					{
						EventHelper.InvokeAsync(this.EventOnlineDoor, door);
					}
				}
			}
		});
	}

	private void UpdateAllDoors(List<SaltoDBDoor> doors)
	{
		Task.Run(delegate
		{
			foreach (SaltoDBDoor door in doors)
			{
				try
				{
					if (door != null && !string.IsNullOrWhiteSpace(door?.Name))
					{
						bool needUpdate = door != null && door.UpdateRequired == 1;
						EventHelper.InvokeAsync(this.EventDoorUpdateReceived, new EventDoorUpdate
						{
							DoorName = door.Name,
							ExtDoorID = door.ExtDoorID,
							BatteryStatus = door.BatteryStatus,
							NeedUpdate = needUpdate
						});
					}
				}
				catch (Exception ex)
				{
					Trace.Error(ex, null, "UpdateAllDoors", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 487);
				}
			}
		});
	}

	internal List<string> GetZonesByDoorExtID(string doorId)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(doorId))
			{
				return new List<string>();
			}
			List<string> list = (from x in _zonesDoors
				where x.Value.Contains(doorId)
				select x.Key).ToList();
			Trace.Debug(string.Format("{0} - {1} found {2} of {3}", "GetZonesByDoorExtID", doorId, list.Count(), _zonesDoors.Count()), "GetZonesByDoorExtID", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 503);
			return list;
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "GetZonesByDoorExtID", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 508);
			return new List<string>();
		}
	}

	private async Task MonitorEvents()
	{
		Trace.Debug($"MonitorEvents through audit trail (LastAuditTrailId={_lastAuditTrailId} / nextEventId={_nextEventId}) - start => will update Door status, cards/reader, operators...", "MonitorEvents", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 515);
		try
		{
			while (!_stop.IsCancellationRequested)
			{
				try
				{
					List<SaltoDBAuditTrailEvent> list = await GetAuditTrailEventsByPackets();
					if (list == null || !list.Any())
					{
						continue;
					}
					Trace.Debug($"MonitorEvents list with {list.Count()} items. Max event ID={list.Max((SaltoDBAuditTrailEvent x) => x.EventID)} (lastAuditTrailId={_lastAuditTrailId}). Can update={list.Max((SaltoDBAuditTrailEvent x) => x.EventID) > _lastAuditTrailId}", "MonitorEvents", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 533);
					foreach (SaltoDBAuditTrailEvent item in list)
					{
						try
						{
							if (item == null)
							{
								Trace.Debug("Monitor event: Null event received!", "MonitorEvents", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 540);
							}
							else if (item.EventID > _lastAuditTrailId)
							{
								_lastAuditTrailId = item.EventID;
								this.LastAuditTrailIdReceived?.Invoke(item.EventID);
								switch (item.SubjectType)
								{
								case 0:
								{
									Trace.Debug($"Audit trail event card {item.DoorID} / {item.EventID} / {item.SubjectID} / {item.Operation}", "MonitorEvents", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 553);
									EventCard o2 = new EventCard
									{
										DoorID = item.DoorID,
										EventID = item.EventID,
										SubjectID = item.SubjectID,
										Operation = item.Operation,
										DateUtc = item.DateUtc
									};
									EventHelper.InvokeAsync(this.EventCardReceived, o2);
									break;
								}
								case 1:
								{
									Trace.Debug($"Audit trail event door (will update status) {item?.DoorID} / evt: {item?.EventID} / subj: {item?.SubjectID} / date: {item?.DateUtc} / op: {item?.Operation}", "MonitorEvents", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 566);
									EventDoor o = new EventDoor
									{
										DoorID = item.DoorID,
										EventID = item.EventID,
										SubjectID = item.SubjectID,
										DateUtc = item.DateUtc
									};
									EventHelper.InvokeAsync(this.EventDoorReceived, o);
									break;
								}
								case 2:
								{
									Trace.Debug($"Audit trail event Operator {item.DoorID} / {item.EventID} / {item.SubjectID} / {item.Operation}", "MonitorEvents", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 572);
									EventOperator o3 = new EventOperator
									{
										DoorID = item.DoorID,
										EventID = item.EventID,
										SubjectID = item.SubjectID,
										DateUtc = item.DateUtc
									};
									EventHelper.InvokeAsync(this.EventOperatorReceived, o3);
									break;
								}
								default:
									Trace.Error("Event type unknown: " + item, "MonitorEvents", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 579);
									break;
								}
							}
						}
						catch (Exception ex)
						{
							Trace.Debug($"Channel.MonitorEvent for exception for eventid= {item?.EventID} / doorID {item?.DoorID} / operation {item?.Operation} / subject {item?.SubjectID} => {ex.Message}", "MonitorEvents", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 585);
							Trace.Error(ex, null, "MonitorEvents", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 586);
						}
					}
				}
				catch (Exception ex2)
				{
					Trace.Error("Channel.MonitorEvent while exception " + ex2.Message, "MonitorEvents", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 594);
					Trace.Debug(ex2, null, "MonitorEvents", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 595);
					if (ex2.StackTrace != null)
					{
						Trace.Debug(ex2.StackTrace, "MonitorEvents", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 597);
					}
				}
				finally
				{
					await Wait(MonitorAuditTrailIntervalInMs, _stop);
				}
			}
		}
		catch (TaskCanceledException ex3)
		{
			Trace.Debug("Channel.MonitorEvent Task Exception" + ex3.Message, "MonitorEvents", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 607);
		}
		catch (Exception ex4)
		{
			Trace.Debug("Channel.MonitorEvent Exception" + ex4.Message, "MonitorEvents", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 611);
			Trace.Error(ex4, null, "MonitorEvents", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 612);
			if (ex4.StackTrace != null)
			{
				Trace.Debug(ex4.StackTrace, "MonitorEvents", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 614);
			}
		}
		Trace.Debug("MonitorEvents - end", "MonitorEvents", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 618);
	}

	private async Task MonitorEventStream()
	{
		Trace.Debug($"Monitor event Stream (get tcp-json infos from salto space) {_eventStreamHost}:{_eventStreamPort}", "MonitorEventStream", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 626);
		try
		{
			while (!_stop.IsCancellationRequested)
			{
				try
				{
					TcpListener listen = new TcpListener(IPAddress.Any, _eventStreamPort);
					listen.Start();
					while (!_stop.IsCancellationRequested)
					{
						using TcpClient client = listen.AcceptTcpClient();
						using (NetworkStream ns = client.GetStream())
						{
							if (client.ReceiveBufferSize >= 1)
							{
								byte[] array = new byte[client.ReceiveBufferSize];
								ns.Read(array, 0, client.ReceiveBufferSize);
								string @string = Encoding.UTF8.GetString(array);
								Trace.Debug("event stream : message received -> " + @string, "MonitorEventStream", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 654);
								ParseStreamEventMessage(array);
								goto end_IL_00ac;
							}
							await Wait(1000, _stop);
							goto end_IL_0097;
							end_IL_00ac:;
						}
						end_IL_0097:;
					}
				}
				catch (TaskCanceledException ex)
				{
					Trace.Debug(ex.Message, "MonitorEventStream", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 662);
				}
				catch (Exception ex2)
				{
					Trace.Error($"Can not start listener for the Monitor event stream - please check that the url is registered in windows and all rights are ok for the driver >> {_eventStreamHost}:{_eventStreamPort}", "MonitorEventStream", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 666);
					Trace.Debug(ex2, null, "MonitorEventStream", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 667);
					await Task.Delay(ConnectionRetryIntervalInMs);
				}
			}
		}
		catch (Exception ex3)
		{
			Trace.Error("MonitorEventStream Exception: " + ex3.Message, "MonitorEventStream", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 675);
			Trace.Error(ex3, null, "MonitorEventStream", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 676);
		}
		Trace.Debug("MonitorEventStream - End", "MonitorEventStream", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 678);
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
			Trace.Error(ex2, null, "Wait", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 700);
		}
	}

	private bool TryParseAddress(string addressin, out string address, out int port)
	{
		address = "";
		port = 0;
		try
		{
			string[] array = addressin.Split(':');
			if (array.Length == 2 && int.TryParse(array[1], out port))
			{
				address = array[0];
				return true;
			}
			return false;
		}
		catch (Exception)
		{
			return false;
		}
	}

	internal async Task<string> Open()
	{
		string text = (await (_ship?.Info?.GetInfoAsync()))?.ProtocolVersion;
		_networkConnection = (CommStatus)((!string.IsNullOrWhiteSpace(text)) ? 1 : 3);
		_clientBridge?.Start();
		return text;
	}

	internal async Task<List<SaltoDBDoor>> GetDoors()
	{
		List<SaltoDBDoor> doors = new List<SaltoDBDoor>();
		int i = 0;
		int numberOfDoors = 10000;
		bool hasMoreDoors = true;
		while (hasMoreDoors)
		{
			SaltoDBDoor[] array = await _ship.SaltoDb.SaltoDBDoorListRead(numberOfDoors, i);
			if (array != null && array.Any())
			{
				i += numberOfDoors;
				doors.AddRange(array);
				if (array.Count() < numberOfDoors)
				{
					hasMoreDoors = false;
				}
			}
		}
		doors.AddRange(await GetLockers(numberOfDoors));
		if (IsSaltoOEM)
		{
			doors.AddRange(await GetHotelRooms(numberOfDoors));
			doors.AddRange(await GetHotelSuites(numberOfDoors));
		}
		_doors = doors;
		return doors;
	}

	private async Task<List<SaltoDBDoor>> GetLockers(int numberOfDoors)
	{
		int i = 0;
		List<SaltoDBDoor> lockers = new List<SaltoDBDoor>();
		SaltoDBLocker[] array;
		do
		{
			array = await _ship.SaltoDb.SaltoDBLockerListRead(numberOfDoors, i);
			i += numberOfDoors;
			lockers.AddRange(array.Select((SaltoDBLocker locker) => (SaltoDBDoor)locker));
		}
		while (array.Length >= numberOfDoors);
		return lockers;
	}

	private async Task<List<SaltoDBDoor>> GetHotelRooms(int numberOfDoors)
	{
		int i = 0;
		List<SaltoDBDoor> hotelRooms = new List<SaltoDBDoor>();
		SaltoDBHotelRoom[] array;
		do
		{
			array = await _ship.SaltoDb.SaltoDBHotelRoomListRead(numberOfDoors, i);
			i += numberOfDoors;
			hotelRooms.AddRange(array.Select((SaltoDBHotelRoom locker) => (SaltoDBDoor)locker));
		}
		while (array.Length >= numberOfDoors);
		return hotelRooms;
	}

	private async Task<List<SaltoDBDoor>> GetHotelSuites(int numberOfDoors)
	{
		int i = 0;
		List<SaltoDBDoor> hotelSuites = new List<SaltoDBDoor>();
		SaltoDBHotelSuite[] array;
		do
		{
			array = await _ship.SaltoDb.SaltoDBHotelSuiteListRead(numberOfDoors, i);
			i += numberOfDoors;
			hotelSuites.AddRange(array.Select((SaltoDBHotelSuite locker) => (SaltoDBDoor)locker));
		}
		while (array.Length >= numberOfDoors);
		return hotelSuites;
	}

	internal async Task<List<OnlineDoorStatus>> GetOnlineDoors()
	{
		List<OnlineDoorStatus> doors = new List<OnlineDoorStatus>();
		OnlineDoorStatus[] array = ((!_useExtIds) ? (await _ship.SaltoDb.GetOnlineDoorStatusByName()) : (await _ship.SaltoDb.GetOnlineDoorStatus()));
		OnlineDoorStatus[] collection = array;
		doors.AddRange(collection);
		return doors;
	}

	internal async Task OpenOnlineZoneCmd(string zoneExtId)
	{
		Trace.Debug("OpenOnlineZoneCmd - " + zoneExtId, "OpenOnlineZoneCmd", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 833);
		if (!string.IsNullOrWhiteSpace(zoneExtId))
		{
			string[] array = _zonesDoors.FirstOrDefault((KeyValuePair<string, List<string>> x) => x.Key == zoneExtId).Value.ToArray();
			if (_useExtIds)
			{
				await _ship.SaltoDb.CmdOnlineDoorOpen(array);
			}
			else
			{
				await _ship.SaltoDb.CmdOnlineDoorOpenByName(array);
			}
		}
	}

	internal async Task CloseOnlineZoneCmd(string zoneExtId)
	{
		Trace.Debug("OpenOnlineZoneCmd - " + zoneExtId, "CloseOnlineZoneCmd", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 846);
		if (!string.IsNullOrWhiteSpace(zoneExtId))
		{
			string[] array = _zonesDoors.FirstOrDefault((KeyValuePair<string, List<string>> x) => x.Key == zoneExtId).Value.ToArray();
			if (_useExtIds)
			{
				await _ship.SaltoDb.CmdOnlineDoorClose(array);
			}
			else
			{
				await _ship.SaltoDb.CmdOnlineDoorCloseByName(array);
			}
		}
	}

	internal async Task EmergencyOpenOnlineZoneCmd(string zoneExtId)
	{
		Trace.Debug("OpenOnlineZoneCmd - " + zoneExtId, "EmergencyOpenOnlineZoneCmd", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 859);
		if (!string.IsNullOrWhiteSpace(zoneExtId))
		{
			string[] array = _zonesDoors.FirstOrDefault((KeyValuePair<string, List<string>> x) => x.Key == zoneExtId).Value.ToArray();
			if (_useExtIds)
			{
				await _ship.SaltoDb.CmdOnlineDoorEmergencyOpen(array);
			}
			else
			{
				await _ship.SaltoDb.CmdOnlineDoorEmergencyOpenByName(array);
			}
		}
	}

	internal async Task EmergencyCloseOnlineZoneCmd(string zoneExtId)
	{
		Trace.Debug("OpenOnlineZoneCmd - " + zoneExtId, "EmergencyCloseOnlineZoneCmd", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 872);
		if (!string.IsNullOrWhiteSpace(zoneExtId))
		{
			string[] array = _zonesDoors.FirstOrDefault((KeyValuePair<string, List<string>> x) => x.Key == zoneExtId).Value.ToArray();
			if (_useExtIds)
			{
				await _ship.SaltoDb.CmdOnlineDoorEmergencyClose(array);
			}
			else
			{
				await _ship.SaltoDb.CmdOnlineDoorEmergencyCloseByName(array);
			}
		}
	}

	internal async Task EndOfEmergencyOnlineZoneCmd(string zoneExtId)
	{
		Trace.Debug("OpenOnlineZoneCmd - " + zoneExtId, "EndOfEmergencyOnlineZoneCmd", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 885);
		if (!string.IsNullOrWhiteSpace(zoneExtId))
		{
			string[] array = _zonesDoors.FirstOrDefault((KeyValuePair<string, List<string>> x) => x.Key == zoneExtId).Value.ToArray();
			if (_useExtIds)
			{
				await _ship.SaltoDb.CmdOnlineDoorEndOfEmergency(array);
			}
			else
			{
				await _ship.SaltoDb.CmdOnlineDoorEndOfEmergencyByName(array);
			}
		}
	}

	internal async Task StartOfficeModeOnlineZoneCmd(string zoneExtId)
	{
		Trace.Debug("OpenOnlineZoneCmd - " + zoneExtId, "StartOfficeModeOnlineZoneCmd", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 898);
		if (!string.IsNullOrWhiteSpace(zoneExtId))
		{
			string[] array = _zonesDoors.FirstOrDefault((KeyValuePair<string, List<string>> x) => x.Key == zoneExtId).Value.ToArray();
			if (_useExtIds)
			{
				await _ship.SaltoDb.CmdOnlineDoorStartOfficeMode(array);
			}
			else
			{
				await _ship.SaltoDb.CmdOnlineDoorStartOfficeModeByName(array);
			}
		}
	}

	internal async Task EndOfficeModeOnlineZoneCmd(string zoneExtId)
	{
		Trace.Debug("OpenOnlineZoneCmd - " + zoneExtId, "EndOfficeModeOnlineZoneCmd", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 911);
		if (!string.IsNullOrWhiteSpace(zoneExtId))
		{
			string[] array = _zonesDoors.FirstOrDefault((KeyValuePair<string, List<string>> x) => x.Key == zoneExtId).Value.ToArray();
			if (_useExtIds)
			{
				await _ship.SaltoDb.CmdOnlineDoorEndOfficeMode(array);
			}
			else
			{
				await _ship.SaltoDb.CmdOnlineDoorEndOfficeModeByName(array);
			}
		}
	}

	internal async Task<bool> CommandOnlineRelays(string[] relays, bool activate)
	{
		Trace.Debug(string.Format("CommandOnlineRelays - activate={0}  - relays={1}", activate, string.Join(",", relays)), "CommandOnlineRelays", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 934);
		bool flag = await _ship.SaltoDb.CmdOnlineRelayByName(relays, activate);
		Trace.Debug(string.Format("CommandOnlineRelays result = {0} with activate={1}  - relays={2}", flag, activate, string.Join(",", relays)), "CommandOnlineRelays", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 936);
		return flag;
	}

	internal async Task OpenOnlineDoorCmd(string extDoorId)
	{
		Trace.Debug($"OpenOnlineDoorCmd - {extDoorId} (use extId={_useExtIds})", "OpenOnlineDoorCmd", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 945);
		if (!string.IsNullOrWhiteSpace(extDoorId))
		{
			if (_useExtIds)
			{
				await _ship.SaltoDb.CmdOnlineDoorOpen(extDoorId);
			}
			else
			{
				await _ship.SaltoDb.CmdOnlineDoorOpenByName(extDoorId);
			}
		}
	}

	internal async Task EmergencyOpenOnlineDoorCmd(string extDoorId)
	{
		Trace.Debug($"EmergencyOpenOnlineDoorCmd - {extDoorId} (use extId={_useExtIds})", "EmergencyOpenOnlineDoorCmd", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 956);
		if (!string.IsNullOrWhiteSpace(extDoorId))
		{
			if (_useExtIds)
			{
				await _ship.SaltoDb.CmdOnlineDoorEmergencyOpen(extDoorId);
			}
			else
			{
				await _ship.SaltoDb.CmdOnlineDoorEmergencyOpenByName(extDoorId);
			}
		}
	}

	internal async Task CloseOnlineDoorCmd(string extDoorId)
	{
		Trace.Debug($"CloseOnlineDoorCmd - {extDoorId}  (use extId={_useExtIds})", "CloseOnlineDoorCmd", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 968);
		if (!string.IsNullOrWhiteSpace(extDoorId))
		{
			if (_useExtIds)
			{
				await _ship.SaltoDb.CmdOnlineDoorClose(extDoorId);
			}
			else
			{
				await _ship.SaltoDb.CmdOnlineDoorCloseByName(extDoorId);
			}
		}
	}

	internal async Task EmergencyCloseOnlineDoorCmd(string extDoorId)
	{
		Trace.Debug($"EmergencyCloseOnlineDoorCmd - {extDoorId}  (use extId={_useExtIds})", "EmergencyCloseOnlineDoorCmd", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 980);
		if (!string.IsNullOrWhiteSpace(extDoorId))
		{
			if (_useExtIds)
			{
				await _ship.SaltoDb.CmdOnlineDoorEmergencyClose(extDoorId);
			}
			else
			{
				await _ship.SaltoDb.CmdOnlineDoorEmergencyCloseByName(extDoorId);
			}
		}
	}

	internal async Task EndOfEmergencyCmd(string extDoorId)
	{
		Trace.Debug("EndOfEmergencyCmd - " + extDoorId, "EndOfEmergencyCmd", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 992);
		if (!string.IsNullOrWhiteSpace(extDoorId))
		{
			if (_useExtIds)
			{
				await _ship.SaltoDb.CmdOnlineDoorEndOfEmergency(extDoorId);
			}
			else
			{
				await _ship.SaltoDb.CmdOnlineDoorEndOfEmergencyByName(extDoorId);
			}
		}
	}

	internal async Task StartOfficeModeCmd(string extDoorId)
	{
		Trace.Debug("EndOfEmergencyCmd - " + extDoorId, "StartOfficeModeCmd", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 1004);
		if (!string.IsNullOrWhiteSpace(extDoorId))
		{
			if (_useExtIds)
			{
				await _ship.SaltoDb.CmdOnlineDoorStartOfficeMode(extDoorId);
			}
			else
			{
				await _ship.SaltoDb.CmdOnlineDoorStartOfficeModeByName(extDoorId);
			}
		}
	}

	internal async Task EndOfficeModeCmd(string extDoorId)
	{
		Trace.Debug("EndOfEmergencyCmd - " + extDoorId, "EndOfficeModeCmd", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 1016);
		if (!string.IsNullOrWhiteSpace(extDoorId))
		{
			if (_useExtIds)
			{
				await _ship.SaltoDb.CmdOnlineDoorEndOfficeMode(extDoorId);
			}
			else
			{
				await _ship.SaltoDb.CmdOnlineDoorEndOfficeModeByName(extDoorId);
			}
		}
	}

	internal async Task<List<SaltoDBOnlineRelay>> GetOnlineRelays()
	{
		List<SaltoDBOnlineRelay> relays = new List<SaltoDBOnlineRelay>();
		int i = 0;
		int numberOfZones = 10000;
		bool hasMoreZones = true;
		while (hasMoreZones)
		{
			SaltoDBOnlineRelay[] array = await _ship.SaltoDb.SaltoDBOnlineRelayListRead(numberOfZones, i);
			if (array != null && array.Any())
			{
				i += numberOfZones;
				relays.AddRange(array);
				if (array.Count() < numberOfZones)
				{
					hasMoreZones = false;
				}
			}
		}
		return relays.ToList();
	}

	internal async Task<List<SaltoDBOnlineInput>> GetOnlineInputs()
	{
		List<SaltoDBOnlineInput> inputs = new List<SaltoDBOnlineInput>();
		int i = 0;
		int numberOfZones = 10000;
		bool hasMoreZones = true;
		while (hasMoreZones)
		{
			SaltoDBOnlineInput[] array = await _ship.SaltoDb.SaltoDBOnlineInputListRead(numberOfZones, i);
			if (array != null && array.Any())
			{
				i += numberOfZones;
				inputs.AddRange(array);
				if (array.Count() < numberOfZones)
				{
					hasMoreZones = false;
				}
			}
		}
		return inputs.ToList();
	}

	internal async Task<List<SaltoDBZone>> GetZones()
	{
		List<SaltoDBZone> zones = new List<SaltoDBZone>();
		int i = 0;
		int numberOfZones = 10000;
		bool hasMoreZones = true;
		while (hasMoreZones)
		{
			SaltoDBZone[] array = await _ship.SaltoDb.SaltoDBZoneListRead(numberOfZones, i);
			if (array != null && array.Any())
			{
				i += numberOfZones;
				zones.AddRange(array);
				if (array.Count() < numberOfZones)
				{
					hasMoreZones = false;
				}
			}
		}
		return zones.ToList();
	}

	internal SaltoDBDoor GetDoor(string doorId)
	{
		return _doors.FirstOrDefault((SaltoDBDoor x) => x.ExtDoorID == doorId);
	}

	internal SaltoDBDoor GetDoorByName(string doorName)
	{
		return _doors.FirstOrDefault((SaltoDBDoor x) => x.Name == doorName);
	}

	private async Task<List<SaltoDBAuditTrailEvent>> GetAuditTrailEventsByPackets(int maxEventByCall = -1)
	{
		if (maxEventByCall < 10)
		{
			maxEventByCall = _maxEventByCall;
		}
		try
		{
			int showDoorId = (_useExtIds ? 1 : 0);
			SaltoDBAuditTrailEvent[] source = await _ship.SaltoDb.SaltoDBAuditTrailRead(_nextEventId, maxEventByCall, 0, showDoorId);
			if (source.Count() > 0)
			{
				_nextEventId = source.Max((SaltoDBAuditTrailEvent x) => x.EventID) + 1;
			}
			return source.ToList();
		}
		catch (Exception ex)
		{
			Trace.Debug($"Channel.GetAuditTrailEventsByPackets Exception => useExtId: {_useExtIds} / nextEventId: {_nextEventId}", "GetAuditTrailEventsByPackets", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 1119);
			Trace.Error(ex, null, "GetAuditTrailEventsByPackets", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 1120);
			return new List<SaltoDBAuditTrailEvent>();
		}
	}

	internal void CancelSaltoKey(string keyId)
	{
		_ship.Encoder.CancelKey(keyId);
	}

	private void ParseStreamEventMessage(byte[] bytes)
	{
		foreach (StreamEvent item in DeserializeEventMessage(bytes))
		{
			EventHelper.InvokeAsync(this.EventStreamReceived, item);
		}
	}

	private List<StreamEvent> DeserializeEventMessage(byte[] bytes)
	{
		List<StreamEvent> list = new List<StreamEvent>();
		string @string = Encoding.UTF8.GetString(bytes);
		try
		{
			MemoryStream memoryStream = new MemoryStream(bytes);
			using (StreamReader json = new StreamReader(memoryStream, detectEncodingFromByteOrderMarks: true))
			{
				foreach (JArray item in Read(json).OfType<JArray>())
				{
					try
					{
						List<StreamEvent> collection = item.ToObject<List<StreamEvent>>();
						list.AddRange(collection);
					}
					catch (Exception)
					{
						Trace.Error("Event stream could not read message : " + item.ToString(), "DeserializeEventMessage", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 1168);
					}
				}
			}
			memoryStream.Flush();
			memoryStream.Close();
		}
		catch (Exception ex2)
		{
			Trace.Error("DeserializeEventMessage ex with " + @string + " >>  " + ex2.Message, "DeserializeEventMessage", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 1184);
			Trace.Debug($"DeserializeEventMessage ex >>  {ex2}", "DeserializeEventMessage", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\ChannelService.cs", 1185);
		}
		return list;
	}

	private IEnumerable Read(StreamReader json)
	{
		JsonTextReader reader = new JsonTextReader(json)
		{
			SupportMultipleContent = true
		};
		JsonSerializer serializer = new JsonSerializer();
		while (reader.Read())
		{
			yield return serializer.Deserialize(reader);
		}
	}
}
