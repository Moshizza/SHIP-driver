using System;
using Prysm.AppVision.Data;

namespace AppDriverShip.Model;

public class ConnectionStatus
{
	public CommStatus NetworkConnection { get; set; }

	public string Message { get; set; }

	public DateTime Timestamp { get; set; } = DateTime.Now;

}
