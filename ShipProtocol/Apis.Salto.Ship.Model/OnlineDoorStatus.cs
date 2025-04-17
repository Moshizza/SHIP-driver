namespace Apis.Salto.Ship.Model;

public class OnlineDoorStatus
{
	public string DoorID { get; set; }

	public int DoorType { get; set; }

	public int CommStatus { get; set; }

	public int DoorStatus { get; set; }

	public int BatteryStatus { get; set; }

	public int TamperStatus { get; set; }

	public string DoorTypeDesc => DoorType switch
	{
		-1 => "unknown", 
		0 => "Control unit (Ip Door)", 
		1 => "RF wireless escutcheon", 
		_ => "", 
	};

	public string CommStatusDesc => CommStatus switch
	{
		-1 => "Unknown", 
		0 => "No communication", 
		1 => "Communication ok", 
		_ => "", 
	};

	public string DoorStatusDesc => DoorStatus switch
	{
		-1 => "Unknown", 
		0 => "Opened", 
		1 => "Closed", 
		2 => "Left opened", 
		3 => "Intrusion", 
		4 => "Emergency open", 
		5 => "Emergency close", 
		6 => "Initializing", 
		_ => "", 
	};

	public string BatteryStatusDesc => BatteryStatus switch
	{
		-1 => "Unknown", 
		0 => "Normal", 
		1 => "Low", 
		2 => "Very low", 
		_ => "", 
	};

	public string TamperStatusDesc => TamperStatus switch
	{
		-1 => "Unknown", 
		0 => "Normal", 
		1 => "Alarmed", 
		_ => "", 
	};

	public override string ToString()
	{
		return "OnlineDoorStatus: id=" + DoorID + " doortype=" + DoorTypeDesc + " comm=" + CommStatusDesc + " door=" + DoorStatusDesc + " battery=" + BatteryStatusDesc + " tamper=" + TamperStatusDesc;
	}
}
