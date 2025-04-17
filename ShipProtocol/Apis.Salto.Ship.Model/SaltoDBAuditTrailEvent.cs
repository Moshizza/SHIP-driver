using System;
using Prysm.AppVision.SDK;

namespace Apis.Salto.Ship.Model;

public class SaltoDBAuditTrailEvent
{
	public int EventID { get; set; }

	public string EventDateTime { get; set; }

	public int Operation { get; set; }

	public bool IsExit { get; set; }

	public string DoorID { get; set; }

	public int SubjectType { get; set; }

	public string SubjectID { get; set; }

	public DateTimeOffset DateUtc
	{
		get
		{
			if (!DateTimeOffset.TryParse(EventDateTime, out var result))
			{
				Trace.Warning("SaltoDBAuditTrailEvent.DateUtc failed to parse date", "DateUtc", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Model\\SaltoDBAuditTrailEvent.cs", 35);
			}
			return result;
		}
	}

	public override string ToString()
	{
		return $"SaltoEvent: EventID={EventID} EventDateTime={EventDateTime} Operation={Operation} IsExit={IsExit} DoorID={DoorID} SubjectType={SubjectType} SubjectID={SubjectID}";
	}
}
