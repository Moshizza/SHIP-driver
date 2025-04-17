using System;
using System.Collections.Generic;
using Prysm.AppVision.Common;

namespace AppDriverShip.Model;

public class StreamEvent
{
	public DateTime EventDateTime { get; set; }

	public DateTime EventDateTimeUTC { get; set; }

	public DateTime EventTime { get; set; }

	public DateTime EventTimeUTC { get; set; }

	public int OperationID { get; set; }

	public string OperationDescription { get; set; }

	public bool IsExit { get; set; }

	public int UserType { get; set; }

	public string UserName { get; set; }

	public string UserExtID { get; set; }

	public string UserGPF1 { get; set; }

	public string UserGPF2 { get; set; }

	public string UserGPF3 { get; set; }

	public string UserGPF4 { get; set; }

	public string UserGPF5 { get; set; }

	public string UserCardSerialNumber { get; set; }

	public string UserCardID { get; set; }

	public string DoorName { get; set; }

	public string DoorExtID { get; set; }

	public string DoorGPF1 { get; set; }

	public string DoorGPF2 { get; set; }

	public List<NameValue> ToNameValue()
	{
		return new List<NameValue>
		{
			NameValue.op_Implicit("EventDateTime=" + EventDateTime.ToString()),
			NameValue.op_Implicit("EventDateTimeUTC=" + EventDateTimeUTC.ToString()),
			NameValue.op_Implicit("EventTime=" + EventTime.ToString()),
			NameValue.op_Implicit("EventTimeUTC=" + EventTimeUTC.ToString()),
			NameValue.op_Implicit("OperationID=" + OperationID),
			NameValue.op_Implicit("OperationDescription=" + OperationDescription),
			NameValue.op_Implicit("IsExit=" + IsExit),
			NameValue.op_Implicit("UserType=" + UserType),
			NameValue.op_Implicit("UserName=" + UserName),
			NameValue.op_Implicit("UserExtID=" + UserExtID),
			NameValue.op_Implicit("UserGPF1=" + UserGPF1),
			NameValue.op_Implicit("UserGPF2=" + UserGPF2),
			NameValue.op_Implicit("UserGPF3=" + UserGPF3),
			NameValue.op_Implicit("UserGPF4=" + UserGPF4),
			NameValue.op_Implicit("UserGPF5=" + UserGPF5),
			NameValue.op_Implicit("UserCardSerialNumber=" + UserCardSerialNumber),
			NameValue.op_Implicit("UserCardID=" + UserCardID),
			NameValue.op_Implicit("DoorName=" + DoorName),
			NameValue.op_Implicit("DoorExtID=" + DoorExtID),
			NameValue.op_Implicit("DoorGPF1=" + DoorGPF1),
			NameValue.op_Implicit("DoorGPF2=" + DoorGPF2)
		};
	}

	public override string ToString()
	{
		return $"userType={UserType}, extId={UserExtID}, OperationId={OperationID}, desc={OperationDescription}, username={UserName}, serialNumber={UserCardSerialNumber}, doorName={DoorName}, doorExtId={DoorExtID}";
	}
}
