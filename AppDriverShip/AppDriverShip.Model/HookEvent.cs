using System;
using System.Collections.Generic;
using Prysm.AppVision.Common;

namespace AppDriverShip.Model;

public class HookEvent
{
	public string RelayName { get; set; }

	public string InputName { get; set; }

	public DateTime UtcDate { get; set; }

	public DateTime LocalDate { get; set; }

	public string OperationName { get; set; }

	public int OperationId { get; set; }

	public string DoorName { get; set; }

	public string DoorExtId { get; set; }

	public string DoorGpf1 { get; set; }

	public string DoorGpf2 { get; set; }

	public string NodeName { get; set; }

	public string UserName { get; set; }

	public string UserGpf1 { get; set; }

	public string UserGpf2 { get; set; }

	public string UserGpf3 { get; set; }

	public string UserGpf4 { get; set; }

	public string UserGpf5 { get; set; }

	public string CardId { get; set; }

	public string CardSerialNumber { get; set; }

	public string DoorIsExit { get; set; }

	public string EventType { get; set; }

	public string CustomInfo1 { get; set; }

	public string CustomInfo2 { get; set; }

	public string CustomInfo3 { get; set; }

	public HookEventType HookEventType => StringExtensions.ToEnum<HookEventType>(EventType, HookEventType.Unknown);

	public override string ToString()
	{
		return $"HookEvent: {EventType} operation={OperationName} ({OperationId}), name={UserName}{RelayName}{InputName} ({DoorExtId})";
	}

	public List<NameValue> ToNameValue()
	{
		return new List<NameValue>
		{
			NameValue.op_Implicit("RelayName=" + RelayName),
			NameValue.op_Implicit("InputName=" + InputName),
			NameValue.op_Implicit("LocalDate=" + LocalDate.ToString()),
			NameValue.op_Implicit("UtcDate=" + UtcDate.ToString()),
			NameValue.op_Implicit("DoorName=" + DoorName),
			NameValue.op_Implicit("CardSerialNumber=" + CardSerialNumber),
			NameValue.op_Implicit("OperationName=" + OperationName),
			NameValue.op_Implicit("OperationId=" + OperationId),
			NameValue.op_Implicit("DoorExtId=" + DoorExtId),
			NameValue.op_Implicit("UserName=" + UserName),
			NameValue.op_Implicit("CardId=" + CardId),
			NameValue.op_Implicit("EventType=" + EventType),
			NameValue.op_Implicit("UserGpf1=" + UserGpf1),
			NameValue.op_Implicit("UserGpf2=" + UserGpf2),
			NameValue.op_Implicit("UserGpf3=" + UserGpf3),
			NameValue.op_Implicit("UserGpf4=" + UserGpf4),
			NameValue.op_Implicit("UserGpf5=" + UserGpf5),
			NameValue.op_Implicit("DoorGpf1=" + DoorGpf1),
			NameValue.op_Implicit("DoorGpf2=" + DoorGpf2),
			NameValue.op_Implicit("DoorIsExit=" + DoorIsExit),
			NameValue.op_Implicit("NodeName=" + NodeName),
			NameValue.op_Implicit("CustomInfo1=" + CustomInfo1),
			NameValue.op_Implicit("CustomInfo2=" + CustomInfo2),
			NameValue.op_Implicit("CustomInfo3=" + CustomInfo3),
			NameValue.op_Implicit("Date=" + LocalDate.ToString())
		};
	}
}
