using System;
using AppDriverShip.Helpers;
using Prysm.AppVision.SDK;

namespace AppDriverShip.Business;

internal class Door
{
	public bool IsZone { get; set; }

	public bool IsOnline { get; set; }

	public int OnlineStatus { get; set; }

	public int OnlineStatus2 { get; set; }

	public string SaltoNameOrId { get; set; }

	public int BatteryLevel { get; set; }

	public bool LowBattery { get; set; }

	public bool UpdateRequired { get; set; }

	public int Status
	{
		get
		{
			return (int)GetStatus();
		}
		set
		{
			if (value != GetStatus())
			{
				bool[] array = value.ToBoolArray();
				if (array != null && array.Length < 2)
				{
					Trace.Debug($"Can not set Door Status for {RowName} with status = {value}. values lenght = {array?.Length}", "Status", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\Door.cs", 40);
					return;
				}
				LowBattery = array[0];
				UpdateRequired = array[1];
			}
		}
	}

	public string RowName { get; set; }

	public Door(string saltoDoorNameOrId, string rowName = "")
	{
		if (string.IsNullOrWhiteSpace(saltoDoorNameOrId))
		{
			throw new ArgumentNullException("Door entity: saltoDoorNameOrId can not be null");
		}
		SaltoNameOrId = saltoDoorNameOrId;
		RowName = rowName;
	}

	public long GetStatus()
	{
		return new bool[2] { LowBattery, UpdateRequired }.ToLong();
	}
}
