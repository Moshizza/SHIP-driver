using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Apis.Salto.Ship;
using Apis.Salto.Ship.Model;
using Prysm.AppVision.AppWizardSalto.Properties;
using Prysm.AppVision.AppWizardSalto.Utils;
using Prysm.AppVision.Common;
using Prysm.AppVision.Data;
using Prysm.AppVision.SDK;
using Salto.Salto.Ship.Model;
using Telerik.Windows.Controls;

namespace Prysm.AppVision.AppWizardSalto.ViewModels;

internal class ViewModel : ObservableObject
{
	private bool _isBusy;

	private string _serverName;

	private const string PROTOCOL_NAME = "SHIP";

	private const string PROTOCOL_EXECUTABLE = "appDriverShip.exe";

	private const string NL = "\r\n";

	private IDictionary<string, VariableRow> _varByName;

	private IDictionary<string, VariableRow> _varByAdrs;

	private List<string> _errors = new List<string>();

	private double _progress;

	private ObservableCollection<string> _details = new ObservableCollection<string>();

	private string _selectedItem;

	private string _errorList;

	private bool _complete;

	private string _res;

	private List<VariableRow> variables = new List<VariableRow>();

	private ShipApi _ship;

	private bool _hasNonAscii;

	private string _badNameExample = "";

	private bool _updateExistingRows;

	private string _nodeDesc;

	private ICommand _symbolsCmd;

	private string _password = "";

	public static Frame Frame { get; set; }

	public bool IsSaltoOEM { get; internal set; }

	public bool IsBusy
	{
		get
		{
			return _isBusy;
		}
		set
		{
			_isBusy = value;
			((ObservableObject)this).NotifyUI("IsBusy");
		}
	}

	public ICommand GoBackCmd => (ICommand)new RelayCommand((Action)GoBack, (Func<bool>)null);

	public double Progress
	{
		get
		{
			return _progress;
		}
		set
		{
			_progress = value;
			((ObservableObject)this).NotifyUI("Progress");
		}
	}

	public ObservableCollection<string> Details
	{
		get
		{
			return _details;
		}
		set
		{
			_details = value;
			((ObservableObject)this).Notify("Details");
		}
	}

	public string SelectedItem
	{
		get
		{
			return _selectedItem;
		}
		set
		{
			_selectedItem = value;
			((ObservableObject)this).Notify("SelectedItem");
		}
	}

	public string ErrorList
	{
		get
		{
			return _errorList;
		}
		set
		{
			_errorList = value;
			((ObservableObject)this).Notify("ErrorList");
		}
	}

	public bool Complete
	{
		get
		{
			return _complete;
		}
		set
		{
			_complete = value;
			((ObservableObject)this).NotifyUI("Complete");
		}
	}

	public string ImportResult
	{
		get
		{
			return _res;
		}
		set
		{
			_res = value;
			((ObservableObject)this).NotifyUI("ImportResult");
		}
	}

	public bool GoBackOnComplete
	{
		get
		{
			if (!Complete)
			{
				return !IsBusy;
			}
			return false;
		}
	}

	public ICommand CreateCmd => (ICommand)new RelayCommand((Action)async delegate
	{
		IsBusy = true;
		await Task.Run(delegate
		{
			try
			{
				variables.Clear();
				Progress = 0.0;
				CreateProtocol();
				Progress = 25.0;
				CreateNodeServer();
				CreateDoors();
				Progress = 50.0;
				Log($"Creating {variables.Count} variables.");
				Log("Import in progress...");
				if (!AppVisionComm.Server.VariableManager.AddVariables(variables.ToArray(), true))
				{
					throw new Exception("Check your licence.");
				}
				Log("Import complete.");
				_errors.Add($"{SelectedEntities.Count()} items imported.");
				_errors.Add($"{variables.Count} variables created.");
				ImportResult = "Import succeed.";
			}
			catch (Exception ex)
			{
				_errors.Add("Import failed: " + ex.Message);
				Log("Import failed: " + ex.Message);
				ObjectExtensions.DebugWriteLine((object)this, true, ex?.ToString() ?? "");
				ImportResult = "Import failed!";
			}
			Progress = 100.0;
			ErrorList = CollectionExtensions.Join<string>((IEnumerable<string>)_errors, "\r\n");
		});
		Complete = true;
		((ObservableObject)this).Notify("GoBackOnComplete");
		IsBusy = false;
	}, (Func<bool>)null);

	public ICommand GoToCompleteCmd => (ICommand)new RelayCommand((Action)delegate
	{
		Navigate("Views/Complete.xaml");
	}, (Func<bool>)null);

	public string AppUsername
	{
		get
		{
			return Settings.Default.AppVisionLogin;
		}
		set
		{
			Settings.Default.AppVisionLogin = value;
			((ObservableObject)this).Notify("AppUsername");
		}
	}

	public string AppHost
	{
		get
		{
			return Settings.Default.AppVisionHost;
		}
		set
		{
			Settings.Default.AppVisionHost = value;
			((ObservableObject)this).Notify("AppHost");
		}
	}

	public ICommand LoginToAppVisionCmd => (ICommand)new RelayCommand((Action<object>)async delegate(object pwdBox)
	{
		IsBusy = true;
		Settings.Default.Save();
		bool num = await AppVisionComm.ConnectAsync(AppHost, AppUsername, ((RadPasswordBox)((pwdBox is RadPasswordBox) ? pwdBox : null)).Password);
		IsBusy = false;
		if (num)
		{
			SystemPrysmBridgePort = Helper.ParseUri(AppVisionComm.Server.Hostname, "http://", 80, "").Port;
			IsSaltoOEM = AppVisionComm.Server.GetLicenseInfo("CodesOEM").Contains("568");
			if (IsSaltoOEM)
			{
				Navigate("Views/AdvancedOptions.xaml");
			}
			else
			{
				Navigate("Views/SystemLogin.xaml");
			}
		}
		else
		{
			MessageBox.Show("Connection failed", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}, (Predicate<object>)delegate(object pwdBox)
	{
		if (!IsBusy)
		{
			string[] obj = new string[3] { AppUsername, AppHost, null };
			object obj2 = ((pwdBox is RadPasswordBox) ? pwdBox : null);
			obj[2] = ((obj2 != null) ? ((RadPasswordBox)obj2).Password : null);
			return !StringExtensions.IsNullOrWhiteSpace(obj);
		}
		return false;
	});

	public string SystemHost
	{
		get
		{
			return Settings.Default.SaltoHostIP;
		}
		set
		{
			Settings.Default.SaltoHostIP = value;
			((ObservableObject)this).Notify("SystemHost");
		}
	}

	public int SystemServerPort
	{
		get
		{
			return Settings.Default.SaltoServerPort;
		}
		set
		{
			Settings.Default.SaltoServerPort = value;
			((ObservableObject)this).Notify("SystemServerPort");
		}
	}

	public int SystemEventPort
	{
		get
		{
			return Settings.Default.SaltoHttpPort;
		}
		set
		{
			Settings.Default.SaltoHttpPort = value;
			((ObservableObject)this).Notify("SystemEventPort");
		}
	}

	public int SystemPrysmBridgePort
	{
		get
		{
			return Settings.Default.PrysmBridgePort;
		}
		set
		{
			Settings.Default.PrysmBridgePort = value;
			((ObservableObject)this).Notify("SystemPrysmBridgePort");
		}
	}

	public int SystemHostPort
	{
		get
		{
			return Settings.Default.SaltoHostPort;
		}
		set
		{
			Settings.Default.SaltoHostPort = value;
			((ObservableObject)this).Notify("SystemHostPort");
		}
	}

	public ICommand LoginToSystemCmd => (ICommand)new RelayCommand((Action<object>)async delegate
	{
		IsBusy = true;
		try
		{
			if (string.IsNullOrWhiteSpace(SystemHost) || SystemServerPort <= 0 || SystemPrysmBridgePort <= 0 || SystemEventPort <= 0 || SystemHostPort <= 0)
			{
				MessageBox.Show("Invalid form. Please fill all fields", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
			else if (SystemServerPort == SystemEventPort || SystemEventPort == SystemHostPort)
			{
				MessageBox.Show("Invalid form. Duplicate use of ports", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
			else if (SystemPrysmBridgePort == SystemServerPort || SystemPrysmBridgePort == SystemEventPort)
			{
				MessageBox.Show($"Invalid port {SystemPrysmBridgePort}. Already taked by server", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
			else
			{
				Settings.Default.Save();
				Entities.Clear();
				await Task.Delay(1).ConfigureAwait(continueOnCapturedContext: false);
				_ship = new ShipApi(SystemHost, SystemServerPort, UseHttps, HttpsUsername, HttpsPassword, HttpsCustomKey);
				if (!UseHttps && string.IsNullOrWhiteSpace((await _ship.Info.GetInfoAsync())?.ProtocolVersion))
				{
					throw new Exception($"Can not connect to Salto server: {SystemHost}:{SystemServerPort}");
				}
				if (await FillAllEntities())
				{
					if (_hasNonAscii)
					{
						MessageBox.Show("Warning: at least one door contains non ascii characters.\nFor example: '" + _badNameExample + "' will be converted to '" + _badNameExample.GetStrongVariableName() + "'", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
					}
					Navigate("Views/Symbols.xaml");
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
		finally
		{
			IsBusy = false;
		}
	}, (Predicate<object>)((object pwd) => !IsBusy && SystemHost != null));

	public ObservableCollection<Entity> Entities { get; } = new ObservableCollection<Entity>();


	public IEnumerable<Entity> SelectedEntities => Entities.Where((Entity e) => e.IsSelected);

	public IEnumerable<Entity> EntitiesInAppVision => Entities.Where((Entity e) => e.AlreadyExist);

	public bool UpdateExistingRows
	{
		get
		{
			return _updateExistingRows;
		}
		set
		{
			_updateExistingRows = value;
			foreach (Entity entity in Entities)
			{
				if (entity.AlreadyExist)
				{
					entity.IsEnabled = value;
				}
			}
			((ObservableObject)this).Notify("UpdateExistingRows");
		}
	}

	public string NodeDesc
	{
		get
		{
			return _nodeDesc;
		}
		set
		{
			_nodeDesc = value;
			((ObservableObject)this).Notify("NodeDesc");
		}
	}

	public ICommand DetailsCmd => (ICommand)new RelayCommand((Action)delegate
	{
		Details.Clear();
		Details.Add($"{SelectedEntities.Count()} item(s) ready for import.");
		CollectionExtensions.AddRange<string>((ICollection<string>)Details, SelectedEntities.Select((Entity e) => e.Name + "\t(" + e.TypeDesc + ")"));
		Navigate("Views/Summary.xaml");
	}, (Func<bool>)(() => !IsBusy && SelectedEntities.Any()));

	public ICommand GoToConnectCmd => (ICommand)new RelayCommand((Action)async delegate
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		if (commandLineArgs.Length > 1)
		{
			try
			{
				Uri uri = Helper.ParseUri(commandLineArgs[1], "http://", 80, "");
				IsBusy = true;
				AppUsername = UriExtensions.GetUsername(uri);
				AppHost = uri.Host + ":" + uri.Port;
				if (await AppVisionComm.ConnectAsync(AppHost, AppUsername, UriExtensions.GetPassword(uri)))
				{
					SystemPrysmBridgePort = Helper.ParseUri(AppVisionComm.Server.Hostname, "http://", 80, "").Port;
					IsSaltoOEM = AppVisionComm.Server.GetLicenseInfo("CodesOEM").Contains("568");
					if (IsSaltoOEM)
					{
						Navigate("Views/AdvancedOptions.xaml");
					}
					else
					{
						Navigate("Views/SystemLogin.xaml");
					}
					return;
				}
			}
			catch (Exception)
			{
			}
			finally
			{
				IsBusy = false;
			}
		}
		Navigate("Views/AppLogin.xaml");
	}, (Func<bool>)null);

	public string OfflineDoorSymbol
	{
		get
		{
			return Settings.Default.OfflineDoorSymbol;
		}
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				if (!value.ToLower().EndsWith(".xaml"))
				{
					value += ".xaml";
				}
				Settings.Default.OfflineDoorSymbol = value.Replace("/", "\\");
				((ObservableObject)this).Notify("OfflineDoorSymbol");
			}
		}
	}

	public string IpDoorSymbol
	{
		get
		{
			return Settings.Default.IpDoorSymbol;
		}
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				if (!value.ToLower().EndsWith(".xaml"))
				{
					value += ".xaml";
				}
				Settings.Default.IpDoorSymbol = value.Replace("/", "\\");
				((ObservableObject)this).Notify("IpDoorSymbol");
			}
		}
	}

	public string RFDoorSymbol
	{
		get
		{
			return Settings.Default.RFDoorSymbol;
		}
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				if (!value.ToLower().EndsWith(".xaml"))
				{
					value += ".xaml";
				}
				Settings.Default.RFDoorSymbol = value.Replace("/", "\\");
				((ObservableObject)this).Notify("RFDoorSymbol");
			}
		}
	}

	public string LockerSymbol
	{
		get
		{
			return Settings.Default.LockerSymbol;
		}
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				Settings.Default.LockerSymbol = StringExtensions.Suffix(value.Replace("/", "\\"), ".xaml");
				((ObservableObject)this).Notify("LockerSymbol");
			}
		}
	}

	public string HotelRoomSymbol
	{
		get
		{
			return Settings.Default.HotelRoomSymbol;
		}
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				Settings.Default.HotelRoomSymbol = StringExtensions.Suffix(value.Replace("/", "\\"), ".xaml");
				((ObservableObject)this).Notify("HotelRoomSymbol");
			}
		}
	}

	public string HotelSuiteSymbol
	{
		get
		{
			return Settings.Default.HotelSuiteSymbol;
		}
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				Settings.Default.HotelSuiteSymbol = StringExtensions.Suffix(value.Replace("/", "\\"), ".xaml");
				((ObservableObject)this).Notify("HotelSuiteSymbol");
			}
		}
	}

	public ICommand SymbolsCmd
	{
		get
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Expected O, but got Unknown
			//IL_0024: Expected O, but got Unknown
			ICommand command = _symbolsCmd;
			if (command == null)
			{
				RelayCommand val = new RelayCommand((Action<object>)delegate
				{
					Settings.Default.Save();
					Navigate("Views/Details.xaml");
				}, (Predicate<object>)null);
				ICommand command2 = (ICommand)val;
				_symbolsCmd = (ICommand)val;
				command = command2;
			}
			return command;
		}
	}

	public bool ImportRelays
	{
		get
		{
			return Settings.Default.ImportRelays;
		}
		set
		{
			Settings.Default.ImportRelays = value;
			((ObservableObject)this).Notify("ImportRelays");
		}
	}

	public bool UseHttps
	{
		get
		{
			return Settings.Default.UseHttps;
		}
		set
		{
			Settings.Default.UseHttps = value;
			if (!value)
			{
				HttpsMode = HttpsMode.None;
			}
			((ObservableObject)this).Notify("UseHttps");
		}
	}

	public bool UseGenericEvent
	{
		get
		{
			return Settings.Default.UseGenericEvent;
		}
		set
		{
			Settings.Default.UseGenericEvent = value;
			((ObservableObject)this).Notify("UseGenericEvent");
		}
	}

	public HttpsMode HttpsMode
	{
		get
		{
			return StringExtensions.ToEnum<HttpsMode>(Settings.Default.HttpsMode, HttpsMode.None);
		}
		set
		{
			Settings.Default.HttpsMode = value.ToString();
			((ObservableObject)this).Notify("HttpsMode");
			switch (value)
			{
			case HttpsMode.None:
				HttpsCustomKey = "";
				HttpsUsername = "";
				HttpsPassword = "";
				break;
			case HttpsMode.Basic:
				HttpsCustomKey = "";
				break;
			case HttpsMode.Custom:
				HttpsUsername = "";
				HttpsPassword = "";
				break;
			}
		}
	}

	public bool UseTcp => !UseHttps;

	public string HttpsUsername
	{
		get
		{
			return Settings.Default.HttpsUsername;
		}
		set
		{
			Settings.Default.HttpsUsername = value;
			((ObservableObject)this).Notify("HttpsUsername");
		}
	}

	public string HttpsPassword
	{
		get
		{
			return _password;
		}
		set
		{
			_password = value;
			((ObservableObject)this).Notify("HttpsPassword");
		}
	}

	public string HttpsCustomKey
	{
		get
		{
			return Settings.Default.HttpsCustomKey;
		}
		set
		{
			Settings.Default.HttpsCustomKey = value;
			((ObservableObject)this).Notify("HttpsCustomKey");
		}
	}

	public ICommand FeaturesCmd => (ICommand)new RelayCommand((Action)delegate
	{
		IsBusy = true;
		try
		{
			Settings.Default.Save();
			if (HttpsMode == HttpsMode.Basic)
			{
				if (string.IsNullOrWhiteSpace(HttpsUsername))
				{
					MessageBox.Show("Please enter a username");
					return;
				}
				if (string.IsNullOrWhiteSpace(HttpsPassword))
				{
					MessageBox.Show("Please enter a password");
					return;
				}
			}
			if (HttpsMode == HttpsMode.Custom && string.IsNullOrWhiteSpace(HttpsCustomKey))
			{
				MessageBox.Show("Please enter the ship https key");
				return;
			}
			Navigate("Views/SystemLogin.xaml");
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
		IsBusy = false;
	}, (Func<bool>)null);

	public ViewModel()
	{
		InitializeAdvancedFeatures();
	}

	public void Navigate(string uri)
	{
		if (!Frame.Dispatcher.CheckAccess())
		{
			Frame.Dispatcher.BeginInvoke((Action)delegate
			{
				Navigate(uri);
			});
		}
		else
		{
			Frame.Navigate(new Uri(uri, UriKind.Relative));
		}
	}

	protected void GoBack()
	{
		if (Frame.CanGoBack)
		{
			Frame.GoBack();
		}
	}

	private void CreateProtocol()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected O, but got Unknown
		if (((IRowStateManager<ProtocolRow, ProtocolState>)(object)AppVisionComm.Server.ProtocolManager).GetRowByName("SHIP") == null)
		{
			ProtocolRow val = new ProtocolRow
			{
				Executable = "appDriverShip.exe",
				AutoStart = true,
				Name = "SHIP"
			};
			Log("Create protocol SHIP");
			if (!AppVisionComm.Server.ProtocolManager.AddProtocol(val))
			{
				Log("Failed to create protocol SHIP");
			}
		}
	}

	private void CreateNodeServer()
	{
		VariableRow[] systemServers = AppVisionComm.GetSystemServers();
		VariableRow val = systemServers.FirstOrDefault(delegate(VariableRow s)
		{
			string address = s.Address;
			return ((address != null) ? address.Split(':')[0] : null) == SystemHost;
		});
		if (val == null)
		{
			CreateRootNode(systemServers);
		}
		else
		{
			_serverName = ((TreeRow)val).Name;
		}
	}

	private void CreateRootNode(VariableRow[] existingServers)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Expected O, but got Unknown
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Expected O, but got Unknown
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_029f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Expected O, but got Unknown
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Expected O, but got Unknown
		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
		//IL_0379: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Expected O, but got Unknown
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f4: Expected O, but got Unknown
		//IL_0430: Unknown result type (might be due to invalid IL or missing references)
		//IL_0435: Unknown result type (might be due to invalid IL or missing references)
		//IL_044b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0456: Unknown result type (might be due to invalid IL or missing references)
		//IL_045d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0466: Expected O, but got Unknown
		_serverName = GetAvailableName(existingServers.Select((VariableRow s) => ((TreeRow)s).Name).ToArray());
		VariableRow val = new VariableRow();
		val.Address = $"{SystemHost}:{SystemServerPort}";
		((TreeRow)val).Name = _serverName;
		((TreeRow)val).Description = "@" + _serverName;
		val.Type = (VariableType)0;
		val.Expression = "$P.SHIP";
		val.Parameters = string.Format("SaltoHostPort={0}{1}", SystemHostPort, "\r\n") + "AlertBatteryLevel=30\r\n" + string.Format("Bridge={0}://+:{1}/DrvSalto{2}", Helper.ParseUri(AppVisionComm.Server.Hostname, "http://", 80, "").Scheme, SystemPrysmBridgePort, "\r\n") + "UsersInSalto=true\r\nEventStreamHost=" + SystemHost + "\r\n" + string.Format("EventStreamPort={0}{1}", SystemEventPort, "\r\n") + "MonitorDoorsIntervalInMs=5000\r\nConnectionRetryIntervalInMs=10000\r\n";
		VariableRow val2 = val;
		if (IsSaltoOEM)
		{
			val = val2;
			val.Parameters = val.Parameters + string.Format("UseHttps={0}{1}", UseHttps, "\r\n") + "HttpsUsername=" + HttpsUsername + "\r\nHttpsPassword=" + HttpsPassword + "\r\nHttpsCustomKey=" + HttpsCustomKey + "\r\n";
		}
		AddVariable(val2);
		VariableRow row = new VariableRow
		{
			Name = _serverName + ".LastAuditTrailId",
			Description = "Internal use. Do not edit",
			Type = (VariableType)2,
			AlarmNeedAcknowledge = false,
			StoreEvent = false,
			StoreValue = false,
			NoVisible = true,
			IsAlarm = false
		};
		AddVariable(row);
		VariableRow row2 = new VariableRow
		{
			Name = _serverName + ".Comm",
			Description = "Salto Space connection",
			Type = (VariableType)4,
			StateDescs = "0:Unknown\r\n1:Connected:d\r\n2:Disconnected\r\n3:Fault:a\r\n4:Downloading",
			IsAlarm = true
		};
		AddVariable(row2);
		VariableRow row3 = new VariableRow
		{
			Name = _serverName + ".Doors",
			Description = "Doors",
			Type = (VariableType)0,
			Parameters = "Type=doors"
		};
		AddVariable(row3);
		if (SelectedEntities.Any((Entity i) => i.SaltoType == SaltoDBType.Locker))
		{
			VariableRow row4 = new VariableRow
			{
				Name = _serverName + ".Lockers",
				Description = "Lockers",
				Type = (VariableType)0,
				Parameters = "Type=lockers"
			};
			AddVariable(row4);
		}
		if (SelectedEntities.Any((Entity i) => i.IsRoom || i.SaltoType == SaltoDBType.HotelRoom || i.SaltoType == SaltoDBType.HotelSuite))
		{
			VariableRow row5 = new VariableRow
			{
				Name = _serverName + ".Hotel",
				Description = "Hotel rooms or suites",
				Type = (VariableType)0,
				Parameters = "Type=hotel"
			};
			AddVariable(row5);
		}
		if (SelectedEntities.Any((Entity i) => i.SaltoType == SaltoDBType.Zone))
		{
			VariableRow row6 = new VariableRow
			{
				Name = _serverName + ".Zones",
				Description = "Areas",
				Type = (VariableType)0,
				Parameters = "Type=zones"
			};
			AddVariable(row6);
		}
		if (IsSaltoOEM && SelectedEntities.Any((Entity i) => i.SaltoType == SaltoDBType.AlarmOutput))
		{
			VariableRow row7 = new VariableRow
			{
				Name = _serverName + ".OnlineRelayCmd",
				Description = "Command online relays",
				Type = (VariableType)3,
				StateType = 1
			};
			AddVariable(row7);
		}
	}

	private void CreateDoors()
	{
		VariableRow[] rowsByName = ((IRowStateManager<VariableRow, VariableState>)(object)AppVisionComm.Server.VariableManager).GetRowsByName(_serverName + ".*");
		_varByName = rowsByName.ToDictionary((VariableRow r) => ((TreeRow)r).Name);
		_varByAdrs = rowsByName.ToLookup((VariableRow r) => r.Address).ToDictionary((IGrouping<string, VariableRow> t) => t.Key ?? "null", (IGrouping<string, VariableRow> t) => t.First());
		foreach (Entity selectedEntity in SelectedEntities)
		{
			try
			{
				switch (selectedEntity.Type)
				{
				case SaltoDBType.HotelSuite:
				case SaltoDBType.HotelRoom:
				case SaltoDBType.Locker:
					CreateOrUpdateDoor(selectedEntity, isOnline: false);
					break;
				case SaltoDBType.AlarmOutput:
					CreateOnlineRelay(selectedEntity);
					break;
				case SaltoDBType.AlarmInput:
					CreateOnlineInputs(selectedEntity);
					break;
				case SaltoDBType.Zone:
					CreateOrUpdateZone(selectedEntity);
					break;
				case SaltoDBType.OfflineDoor:
					CreateOrUpdateDoor(selectedEntity, isOnline: false);
					break;
				case SaltoDBType.IPDoor:
				case SaltoDBType.RFDoor:
					CreateOrUpdateDoor(selectedEntity, isOnline: true);
					break;
				default:
					Log($"Unknwon entity '{selectedEntity.Name}' (ExtId={selectedEntity.ExtId}) > type={selectedEntity.Type}");
					break;
				}
			}
			catch (Exception ex)
			{
				_errors.Add("Error creating '" + selectedEntity?.Name + "': " + ex.Message);
				Log($"Error creating '{selectedEntity?.Name}': {ex}");
			}
		}
	}

	private void CreateOnlineRelay(Entity output)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		VariableRow val = (VariableRow)(((object)CollectionExtensions.GetValueOrDefault<string, VariableRow>(_varByAdrs, output.ExtId, (VariableRow)null)) ?? ((object)new VariableRow
		{
			Name = _serverName + ".AlarmOutputs." + CleanDoorName(output.Name)
		}));
		((TreeRow)val).Description = output.GetVarDesc();
		val.Address = output.ExtId;
		val.Type = (VariableType)0;
		val.Parameters = "ExtId=" + output.ExtId + "\r\nRelayName=" + output.Name;
		AddVariable(val);
		VariableRow row = (VariableRow)(((object)CollectionExtensions.GetValueOrDefault<string, VariableRow>(_varByName, ((TreeRow)val).Name + ".Cmd", (VariableRow)null)) ?? ((object)new VariableRow
		{
			Name = ((TreeRow)val).Name + ".Cmd",
			Description = "Command",
			Type = (VariableType)4,
			StateDescs = "0:Deactivate\r\n1:Activate",
			StateType = 1
		}));
		AddVariable(row);
	}

	private void CreateOnlineInputs(Entity input)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		VariableRow val = (VariableRow)(((object)CollectionExtensions.GetValueOrDefault<string, VariableRow>(_varByAdrs, input.ExtId, (VariableRow)null)) ?? ((object)new VariableRow
		{
			Name = _serverName + ".AlarmInputs." + CleanDoorName(input.Name)
		}));
		((TreeRow)val).Description = input.GetVarDesc();
		val.Address = input.ExtId;
		val.Type = (VariableType)4;
		val.StateDescs = "0:Deactivated\r\n1:Activated";
		val.Parameters = "ExtId=" + input.ExtId + "\r\nInputName=" + input.Name;
		AddVariable(val);
	}

	private void CreateOrUpdateZone(Entity zone)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		VariableRow val = (VariableRow)(((object)CollectionExtensions.GetValueOrDefault<string, VariableRow>(_varByAdrs, zone.ExtId, (VariableRow)null)) ?? ((object)new VariableRow
		{
			Name = _serverName + ".Zones." + CleanDoorName(zone.Name)
		}));
		((TreeRow)val).Description = zone.GetVarDesc();
		val.Type = (VariableType)0;
		val.Address = zone.ExtId;
		val.Parameters = "Name=" + zone.Name + "\r\nExtId=" + zone.ExtId;
		AddVariable(val);
		VariableRow row = (VariableRow)(((object)CollectionExtensions.GetValueOrDefault<string, VariableRow>(_varByName, ((TreeRow)val).Name + ".Cmd", (VariableRow)null)) ?? ((object)new VariableRow
		{
			Name = ((TreeRow)val).Name + ".Cmd",
			Description = "Command",
			Type = (VariableType)4,
			StateDescs = "0:Open\r\n2:Emergency open\r\n3:Emergency close\r\n4:End of emergency\r\n5:Start office mode\r\n6:End office mode",
			StateType = 1
		}));
		AddVariable(row);
	}

	private async void CreateOrUpdateDoor(Entity door, bool isOnline)
	{
		isOnline = isOnline || door.IsOnline;
		VariableRow valueOrDefault = CollectionExtensions.GetValueOrDefault<string, VariableRow>(_varByAdrs, door.ExtId, (VariableRow)null);
		string text = "";
		string symbol = ((!isOnline) ? OfflineDoorSymbol : ((door.Type == SaltoDBType.IPDoor) ? IpDoorSymbol : RFDoorSymbol));
		string text2;
		if (door.IsRoom)
		{
			text2 = "Hotel";
			text = text + "\r\nParentExtID=" + door.ExtParentId + "\r\n";
			symbol = HotelRoomSymbol;
		}
		else
		{
			switch (door.SaltoType)
			{
			case SaltoDBType.Locker:
				text2 = "Lockers";
				break;
			case SaltoDBType.HotelRoom:
				text2 = "Hotel";
				text = text + "\r\nParentExtID=" + door.ExtParentId + "\r\n";
				symbol = HotelRoomSymbol;
				break;
			case SaltoDBType.HotelSuite:
				text2 = "Hotel";
				symbol = HotelSuiteSymbol;
				break;
			default:
				text2 = "Doors";
				break;
			}
		}
		string name = _serverName + "." + text2 + "." + CleanDoorName(door.Name);
		VariableRow node = (VariableRow)(((object)valueOrDefault) ?? ((object)new VariableRow
		{
			Name = name
		}));
		((TreeRow)node).Description = door.GetVarDesc();
		node.Type = (VariableType)0;
		node.Symbol = symbol;
		node.Address = door.ExtId;
		node.Parameters = $"Name={door.Name}\r\nExtId={door.ExtId}\r\nIsOnline={isOnline}\r\n{text}";
		AddVariable(node);
		VariableRow val = (VariableRow)(((object)CollectionExtensions.GetValueOrDefault<string, VariableRow>(_varByName, ((TreeRow)node).Name + ".Reader", (VariableRow)null)) ?? ((object)new VariableRow
		{
			Name = ((TreeRow)node).Name + ".Reader",
			Description = "Reader",
			Type = (VariableType)4
		}));
		if (isOnline)
		{
			val.StateDescs = "0:Updating badge event\r\n10:Access granted\r\n40:Access granted unknown\r\n50:Access denied\r\n56:Key expired\r\n60:Access denied unknown\r\n56:Access denied inactive\r\n58:Access denied period\r\n57:Access denied blacklist";
		}
		else
		{
			val.StateDescs = "10:Access granted\r\n40:Access granted unknown\r\n50:Access denied\r\n56:Key expired\r\n60:Access denied unknown";
		}
		AddVariable(val);
		VariableRow val2 = (VariableRow)(((object)CollectionExtensions.GetValueOrDefault<string, VariableRow>(_varByName, ((TreeRow)node).Name + ".Status", (VariableRow)null)) ?? ((object)new VariableRow
		{
			Name = ((TreeRow)node).Name + ".Status",
			Description = "Status",
			Type = (VariableType)4,
			IsAlarm = isOnline
		}));
		if (isOnline)
		{
			val2.StateDescs = "0:Unknown\r\n1:Door opened\r\n2:Door closed:d\r\n3:Left opened:a\r\n4:Intrusion:a\r\n5:Emergency open\r\n6:Emergency close\r\n7:Initializing";
		}
		else
		{
			val2.StateDescs = "0:Lock ready\r\nb0:Low battery\r\nb1:Update required";
		}
		AddVariable(val2);
		if (UseGenericEvent)
		{
			VariableRow val3 = (VariableRow)(((object)CollectionExtensions.GetValueOrDefault<string, VariableRow>(_varByName, ((TreeRow)node).Name + ".Event", (VariableRow)null)) ?? ((object)new VariableRow
			{
				Name = ((TreeRow)node).Name + ".Event",
				Description = "Event",
				Type = (VariableType)4,
				IsAlarm = false
			}));
			val3.StateDescs = "\r\n1:Node input activated\r\n2:Node input deactivated\r\n3:Short-circuit detected on node input\r\n4:Open-circuit detected on node input\r\n5:Timed relay automatically activated\r\n6:Timed relay automatically deactivated\r\n7:Relay activated by key\r\n8:New renovation code for key\r\n16:Door opened inside handle\r\n17:Door opened Key\r\n18:Door opened key and keyboard\r\n19:Door opened multiple guest key\r\n20:Door opened unique opening\r\n21:Door opened switch\r\n22:Door opened mechanical key\r\n25:Door opened PPD\r\n26:Door opened keyboard\r\n27:Door opened spare card\r\n28:Door opened online command\r\n29:Door opened door most probably opened by a key or PIN\r\n32:End of office mode\r\n33:Door closed key\r\n34:Door closed key and keyboard\r\n35:Door closed keyboard\r\n36:Door closed switch\r\n37:Key inserted\r\n38:Key removed\r\n39:Room prepared\r\n40:Start of privacy\r\n41:End of privacy\r\n42:Duress alarm/node updated\r\n47:Communication with reader lost\r\n48:Communication with reader restored\r\n49:Start of office mode\r\n50:End of office mode\r\n54:Door programmed with spare key\r\n55:New hotel guest key\r\n56:Start of forced opening\r\n57:End of forced opening\r\n58:Start of forced closing\r\n59:End of forced closing\r\n60:Alarm intrusion\r\n61:Alarm tamper\r\n63:End of DLO\r\n64:End of intrusion\r\n65:Start of office mode (online)\r\n66:End of office mode (online)\r\n67:End of tamper\r\n69:Key updated in out of site mode\r\n70:Expiration automatically extended (offline)\r\n72:Online peripheral updated\r\n76:Key updated (online)\r\n78:Key deleted (online)\r\n79:Door has lost communication with the salto software\r\n80:Door has re-established communication with the salto software\r\n81:Opening not allowed - key not activated\r\n82:Opening not allowed - key expired\r\n83:Opening not allowed - key out of date\r\n84:Opening not allowed - key not allowed in this door\r\n85:Opening not allowed - key out of time\r\n87:Opening not allowed - key does not override privacy\r\n88:Opening not allowed - old guest key\r\n89:Opening not allowed - hotel guest key cancelled\r\n90:Opening not allowed - antipassback\r\n92:Opening not allowed - no associated authorization\r\n93:Opening not allowed - invalid pin\r\n95:Opening not allowed - door in emergency state\r\n96:Opening not allowed - key cancelled\r\n97:Opening not allowed - unique opening key already used\r\n98:Opening not allowed - key with old renovation number\r\n99:Warning- key has not been completely updated\r\n100:Opening not allowed - run out of battery\r\n101:Opening not allowed - unable to audit the key\r\n102:Opening not allowed - locker occupancy timeout\r\n103:Opening not allowed - denied by host\r\n104:Blacklisted key deleted\r\n107:Opening not allowed - key with data manipulated\r\n111:Closing not allowed- door in emergency state\r\n112:New renovation code\r\n113:PPD connection\r\n114:Time modified\r\n115:Incorrectly powered\r\n116:Incorrect clock value\r\n117:date and time updated\r\n118:RF lock updated\r\n119:Unable to perform Open/close operation due to a hardware failure\r\n120:Lock/node restarted\r\n1000:Com re-established with the door\r\n1001:Com lost with the door\r\n";
			AddVariable(val3);
		}
		if (isOnline)
		{
			VariableRow row = (VariableRow)(((object)CollectionExtensions.GetValueOrDefault<string, VariableRow>(_varByName, ((TreeRow)node).Name + ".Comm", (VariableRow)null)) ?? ((object)new VariableRow
			{
				Name = ((TreeRow)node).Name + ".Comm",
				Description = "Comm",
				Type = (VariableType)4,
				StateDescs = "0:Normal\r\nb0:Unknown\r\nb1:Comm fault\r\nb2:Low battery\r\nb3:Tamper:a",
				IsAlarm = true
			}));
			AddVariable(row);
			VariableRow row2 = (VariableRow)(((object)CollectionExtensions.GetValueOrDefault<string, VariableRow>(_varByName, ((valueOrDefault != null) ? ((TreeRow)valueOrDefault).Name : null) + ".Cmd", (VariableRow)null)) ?? ((object)new VariableRow
			{
				Name = ((TreeRow)node).Name + ".Cmd",
				Description = "Command",
				Type = (VariableType)4,
				StateDescs = "0:Open\r\n2:Emergency open\r\n3:Emergency close\r\n4:End of emergency\r\n5:Start office mode\r\n6:End office mode",
				StateType = 1
			}));
			AddVariable(row2);
			return;
		}
		if (_varByName.TryGetValue(((TreeRow)node).Name + ".Comm", out var comm))
		{
			Log("Delete variable : " + ((TreeRow)comm).Name);
			if (!(await Task.Run(() => AppVisionComm.Server.VariableManager.DeleteVariable(((TreeRow)comm).Name))))
			{
				Log("Failed to delete " + ((TreeRow)node).Name + ".Comm");
			}
		}
		if (_varByName.TryGetValue(((TreeRow)node).Name + ".Cmd", out var value))
		{
			Log("Delete variable : " + ((TreeRow)value).Name);
			if (!AppVisionComm.Server.VariableManager.DeleteVariable(((TreeRow)value).Name))
			{
				Log("Failed to delete " + ((TreeRow)node).Name + ".Cmd");
			}
		}
	}

	private void AddVariable(VariableRow row)
	{
		Log("Add variable " + ((TreeRow)row).Name);
		variables.Add(row);
	}

	private string CleanDoorName(string name)
	{
		string safename = name.GetStrongVariableName();
		if (string.IsNullOrWhiteSpace(safename))
		{
			safename = Guid.NewGuid().ToString().Replace('-', '_');
		}
		while (variables.Any((VariableRow i) => StringExtensions.EqualsIgnoreCase(StringExtensions.GetShortName(((TreeRow)i).Name, "."), safename)))
		{
			do
			{
				safename = Helper.IncrementName(safename);
			}
			while (_varByName.Keys.Any((string i) => StringExtensions.EqualsIgnoreCase(StringExtensions.GetShortName(i, "."), safename)));
		}
		if (safename != name)
		{
			_errors.Add("\"" + name + "\" has been converted to \"" + safename + "\".");
		}
		return safename;
	}

	private void Log(string log)
	{
		string l = DateTime.Now.ToString("dd/MM/yy hh:mm") + " " + log;
		ObjectExtensions.DebugWriteLine((object)this, true, l);
		DispatcherExtensions.BeginInvoke(Application.Current.Dispatcher, (Action)delegate
		{
			Details.Add(l);
			SelectedItem = l;
		});
	}

	private string GetAvailableName(string[] names)
	{
		string text = "Salto01";
		while (names.Contains(text))
		{
			text = Helper.IncrementName(text);
		}
		return text;
	}

	private bool CheckAscii(string value)
	{
		bool num = value.GetStrongVariableName() == value;
		if (!num)
		{
			_hasNonAscii = true;
			_badNameExample = value;
		}
		return num;
	}

	private async Task<bool> FillAllEntities()
	{
		_ = 5;
		try
		{
			SaltoDBDoor[] doors = await _ship.SaltoDb.SaltoDBDoorListRead(0);
			OnlineDoorStatus[] onlineDoors = await _ship.SaltoDb.GetOnlineDoorStatus();
			string[] ids = GetExistingIds();
			FillDoorEntities(doors, onlineDoors, ids);
			await FillLockersEntities(ids, onlineDoors);
			await FillZonesEntities(ids);
			if (ImportRelays && IsSaltoOEM)
			{
				await FillRelaysAndInputsEntities();
			}
			if (IsSaltoOEM)
			{
				await FillHotelEntities(ids, onlineDoors);
			}
			return true;
		}
		catch (Exception ex)
		{
			string text = ex.Message;
			if (ex.InnerException?.Message != null)
			{
				text = text + " >> " + ex.InnerException.Message;
			}
			MessageBox.Show("Exception >> " + text, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			return false;
		}
	}

	private async Task FillRelaysAndInputsEntities()
	{
		SaltoDBOnlineRelay[] relays = await _ship.SaltoDb.SaltoDBOnlineRelayListRead(1000);
		SaltoDBOnlineInput[] array = await _ship.SaltoDb.SaltoDBOnlineInputListRead(1000);
		string[] existingRelaysAndInputsIds = GetExistingRelaysAndInputsIds("AlarmOutputs");
		string[] existingRelaysAndInputsIds2 = GetExistingRelaysAndInputsIds("AlarmInputs");
		SaltoDBOnlineRelay[] array2 = relays;
		foreach (SaltoDBOnlineRelay ent in array2)
		{
			Entity entity = new Entity();
			entity.Name = ent.Name;
			entity.ExtId = ent.ExtOnlineRelayID;
			entity.Type = SaltoDBType.AlarmOutput;
			if (existingRelaysAndInputsIds.Any((string x) => x == ent.ExtOnlineRelayID))
			{
				entity.AlreadyExist = true;
				entity.IsEnabled = false;
			}
			Entities.Add(entity);
			CheckAscii(entity.Name);
		}
		SaltoDBOnlineInput[] array3 = array;
		foreach (SaltoDBOnlineInput ent in array3)
		{
			Entity entity2 = new Entity();
			entity2.Name = ent.Name;
			entity2.ExtId = ent.ExtOnlineInputID;
			entity2.Type = SaltoDBType.AlarmInput;
			if (existingRelaysAndInputsIds2.Any((string x) => x == ent.ExtOnlineInputID))
			{
				entity2.AlreadyExist = true;
				entity2.IsEnabled = false;
			}
			Entities.Add(entity2);
			CheckAscii(entity2.Name);
		}
	}

	private async Task FillZonesEntities(string[] ids)
	{
		SaltoDBZone[] array = await _ship.SaltoDb.SaltoDBZoneListRead(0);
		foreach (SaltoDBZone zone in array)
		{
			if (!Entities.Any((Entity x) => x.ExtId == zone.ExtZoneID))
			{
				Entity entity = new Entity
				{
					Name = zone.Name,
					Description = zone.Description,
					ExtId = zone.ExtZoneID,
					Type = SaltoDBType.Zone
				};
				if (!string.IsNullOrWhiteSpace(ids.SingleOrDefault((string x) => x == zone.ExtZoneID)))
				{
					entity.AlreadyExist = true;
					entity.IsEnabled = false;
				}
				Entities.Add(entity);
				CheckAscii(entity.Name);
			}
		}
	}

	private async Task FillLockersEntities(string[] ids, OnlineDoorStatus[] onlineDoors)
	{
		SaltoDBLocker[] array = await _ship.SaltoDb.SaltoDBLockerListRead(0);
		foreach (SaltoDBLocker locker in array)
		{
			if (!Entities.Any((Entity x) => x.ExtId == locker.ExtDoorID))
			{
				Entity entity = new Entity();
				entity.Name = locker.Name;
				entity.ExtId = locker.ExtDoorID;
				entity.Description = locker.Description;
				entity.Type = SaltoDBType.Locker;
				if (!string.IsNullOrWhiteSpace(ids.SingleOrDefault((string x) => x == locker.ExtDoorID)))
				{
					entity.AlreadyExist = true;
					entity.IsEnabled = false;
				}
				OnlineDoorStatus onlineDoorStatus = onlineDoors.SingleOrDefault((OnlineDoorStatus x) => x.DoorID == locker.ExtDoorID || (x.DoorID?.ToLower() == locker.Name?.ToLower() && !string.IsNullOrWhiteSpace(x.DoorID)));
				if (onlineDoorStatus != null)
				{
					entity.Type = (SaltoDBType)onlineDoorStatus.DoorType;
					entity.IsOnline = true;
				}
				Entities.Add(entity);
				CheckAscii(entity.Name);
			}
		}
	}

	private async Task FillHotelEntities(string[] ids, OnlineDoorStatus[] onlineDoors)
	{
		SaltoDBHotelRoom[] array = await _ship.SaltoDb.SaltoDBHotelRoomListRead(0);
		foreach (SaltoDBHotelRoom room in array)
		{
			if (!Entities.Any((Entity x) => x.ExtId == room.ExtDoorID))
			{
				Entity entity = new Entity
				{
					Name = room.Name,
					ExtId = room.ExtDoorID,
					Description = room.Description,
					Type = SaltoDBType.HotelRoom,
					ExtParentId = room.ExtParentHotelSuiteID,
					IsRoom = true
				};
				if (!string.IsNullOrWhiteSpace(ids.SingleOrDefault((string x) => x == room.ExtDoorID)))
				{
					entity.AlreadyExist = true;
					entity.IsEnabled = false;
				}
				OnlineDoorStatus onlineDoorStatus = onlineDoors.SingleOrDefault((OnlineDoorStatus x) => x.DoorID == room.ExtDoorID || (x.DoorID?.ToLower() == room.Name?.ToLower() && !string.IsNullOrWhiteSpace(x.DoorID)));
				if (onlineDoorStatus != null)
				{
					entity.Type = (SaltoDBType)onlineDoorStatus.DoorType;
					entity.IsOnline = true;
				}
				Entities.Add(entity);
				CheckAscii(entity.Name);
			}
		}
		SaltoDBHotelSuite[] array2 = await _ship.SaltoDb.SaltoDBHotelSuiteListRead(0);
		foreach (SaltoDBHotelSuite suite in array2)
		{
			if (!Entities.Any((Entity x) => x.ExtId == suite.ExtDoorID))
			{
				Entity entity2 = new Entity
				{
					Name = suite.Name,
					ExtId = suite.ExtDoorID,
					Description = suite.Description,
					Type = SaltoDBType.HotelSuite,
					IsRoom = true
				};
				if (!string.IsNullOrWhiteSpace(ids.SingleOrDefault((string x) => x == suite.ExtDoorID)))
				{
					entity2.AlreadyExist = true;
					entity2.IsEnabled = false;
				}
				OnlineDoorStatus onlineDoorStatus2 = onlineDoors.SingleOrDefault((OnlineDoorStatus x) => x.DoorID == suite.ExtDoorID || (x.DoorID?.ToLower() == suite.Name?.ToLower() && !string.IsNullOrWhiteSpace(x.DoorID)));
				if (onlineDoorStatus2 != null)
				{
					entity2.Type = (SaltoDBType)onlineDoorStatus2.DoorType;
					entity2.IsOnline = true;
				}
				Entities.Add(entity2);
				CheckAscii(entity2.Name);
			}
		}
	}

	private void FillDoorEntities(SaltoDBDoor[] doors, OnlineDoorStatus[] onlineDoors, string[] ids)
	{
		foreach (SaltoDBDoor door in doors)
		{
			if (!Entities.Any((Entity x) => x.ExtId == door.ExtDoorID))
			{
				Entity entity = new Entity
				{
					Name = door.Name,
					ExtId = door.ExtDoorID,
					Description = door.Description
				};
				OnlineDoorStatus onlineDoorStatus = onlineDoors.SingleOrDefault((OnlineDoorStatus x) => x.DoorID == door.ExtDoorID || (x.DoorID?.ToLower() == door.Name?.ToLower() && !string.IsNullOrWhiteSpace(x.DoorID)));
				if (onlineDoorStatus != null)
				{
					entity.Type = (SaltoDBType)onlineDoorStatus.DoorType;
					entity.IsOnline = true;
				}
				if (!string.IsNullOrWhiteSpace(ids.SingleOrDefault((string x) => x == door.ExtDoorID)))
				{
					entity.AlreadyExist = true;
					entity.IsEnabled = false;
				}
				Entities.Add(entity);
				CheckAscii(entity.Name);
			}
		}
	}

	private string[] GetExistingRelaysAndInputsIds(string parameterName)
	{
		try
		{
			VariableRow val = AppVisionComm.GetSystemServers()?.Where((VariableRow x) => x.Address != null && x.Address.Contains(":")).SingleOrDefault((VariableRow s) => s.Address.Split(':').FirstOrDefault() == SystemHost);
			if (val != null)
			{
				return (from x in AppVisionComm.Server.VariableManager.GetRowsByFilter(new string[1] { "$V." + ((TreeRow)val).Name + "." + parameterName + ".*" })
					where !string.IsNullOrWhiteSpace(x.Address)
					select x.Address).ToArray();
			}
		}
		catch (Exception ex)
		{
			string text = ex.Message;
			if (ex.InnerException?.Message != null)
			{
				text = text + " >> " + ex.InnerException.Message;
			}
			MessageBox.Show("Exception >> " + text, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
		return new string[0];
	}

	private string[] GetExistingIds()
	{
		try
		{
			VariableRow[] array = AppVisionComm.GetSystemServers();
			if (array == null || !array.Any())
			{
				AppServer server = AppVisionComm.Server;
				array = ((server == null) ? null : ((IRowStateManager<VariableRow, VariableState>)(object)server.VariableManager)?.GetRowsByName("$V.*|Type=Node")?.Where(delegate(VariableRow x)
				{
					if (x == null)
					{
						return false;
					}
					string name = ((TreeRow)x).Name;
					return ((name != null) ? new bool?(!name.Contains(".")) : null) == true;
				})?.ToArray());
			}
			VariableRow val = array?.Where((VariableRow x) => x.Address != null && x.Address.Contains(":")).SingleOrDefault((VariableRow s) => s.Address.Split(':').FirstOrDefault() == SystemHost);
			if (val != null)
			{
				return (from x in AppVisionComm.Server.VariableManager.GetRowsByFilter(new string[1] { "$V." + ((TreeRow)val).Name + ".*|Type=Node" })
					select x.Address).ToArray();
			}
		}
		catch (Exception ex)
		{
			string text = ex.Message;
			if (ex.InnerException?.Message != null)
			{
				text = text + " >> " + ex.InnerException.Message;
			}
			MessageBox.Show("Exception >> " + text, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
		return new string[0];
	}

	private void InitializeAdvancedFeatures()
	{
	}
}
