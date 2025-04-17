using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Prysm.AppVision.AppWizardSalto.Properties;

[CompilerGenerated]
[GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.4.0.0")]
internal sealed class Settings : ApplicationSettingsBase
{
	private static Settings defaultInstance = (Settings)SettingsBase.Synchronized(new Settings());

	public static Settings Default => defaultInstance;

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("localhost")]
	public string AppVisionHost
	{
		get
		{
			return (string)this["AppVisionHost"];
		}
		set
		{
			this["AppVisionHost"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("ADM")]
	public string AppVisionLogin
	{
		get
		{
			return (string)this["AppVisionLogin"];
		}
		set
		{
			this["AppVisionLogin"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("80")]
	public int PrysmBridgePort
	{
		get
		{
			return (int)this["PrysmBridgePort"];
		}
		set
		{
			this["PrysmBridgePort"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("localhost")]
	public string SaltoHostIP
	{
		get
		{
			return (string)this["SaltoHostIP"];
		}
		set
		{
			this["SaltoHostIP"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("6500")]
	public int SaltoServerPort
	{
		get
		{
			return (int)this["SaltoServerPort"];
		}
		set
		{
			this["SaltoServerPort"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("8081")]
	public int SaltoHostPort
	{
		get
		{
			return (int)this["SaltoHostPort"];
		}
		set
		{
			this["SaltoHostPort"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("6501")]
	public int SaltoHttpPort
	{
		get
		{
			return (int)this["SaltoHttpPort"];
		}
		set
		{
			this["SaltoHttpPort"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("Symbols\\SaltoHandle.xaml")]
	public string OfflineDoorSymbol
	{
		get
		{
			return (string)this["OfflineDoorSymbol"];
		}
		set
		{
			this["OfflineDoorSymbol"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("Symbols\\SaltoDoor.xaml")]
	public string IpDoorSymbol
	{
		get
		{
			return (string)this["IpDoorSymbol"];
		}
		set
		{
			this["IpDoorSymbol"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool UseHttps
	{
		get
		{
			return (bool)this["UseHttps"];
		}
		set
		{
			this["UseHttps"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool ImportRelays
	{
		get
		{
			return (bool)this["ImportRelays"];
		}
		set
		{
			this["ImportRelays"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("None")]
	public string HttpsMode
	{
		get
		{
			return (string)this["HttpsMode"];
		}
		set
		{
			this["HttpsMode"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("")]
	public string HttpsUsername
	{
		get
		{
			return (string)this["HttpsUsername"];
		}
		set
		{
			this["HttpsUsername"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("")]
	public string HttpsCustomKey
	{
		get
		{
			return (string)this["HttpsCustomKey"];
		}
		set
		{
			this["HttpsCustomKey"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("Symbols\\SaltoHandleRF.xaml")]
	public string RFDoorSymbol
	{
		get
		{
			return (string)this["RFDoorSymbol"];
		}
		set
		{
			this["RFDoorSymbol"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("Symbols\\SaltoLocker.xaml")]
	public string LockerSymbol
	{
		get
		{
			return (string)this["LockerSymbol"];
		}
		set
		{
			this["LockerSymbol"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("Symbols\\SaltoHotelRoom.xaml")]
	public string HotelRoomSymbol
	{
		get
		{
			return (string)this["HotelRoomSymbol"];
		}
		set
		{
			this["HotelRoomSymbol"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("Symbols\\SaltoHotelSuite.xaml")]
	public string HotelSuiteSymbol
	{
		get
		{
			return (string)this["HotelSuiteSymbol"];
		}
		set
		{
			this["HotelSuiteSymbol"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool UseGenericEvent
	{
		get
		{
			return (bool)this["UseGenericEvent"];
		}
		set
		{
			this["UseGenericEvent"] = value;
		}
	}
}
