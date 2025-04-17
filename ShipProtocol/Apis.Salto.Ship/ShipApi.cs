using System;
using Apis.Salto.Ship.Api;
using AppDriverShip.Ship;

namespace Apis.Salto.Ship;

public class ShipApi
{
	private readonly ShipTransmitter _tx;

	public ApiInfo Info { get; }

	public SaltoDb_OnlineDoors SaltoDb { get; }

	public Encoder Encoder { get; }

	public ShipApi(string address, int port, bool useHttps, string basicUsername = "", string basicPassword = "", string saltoShipHttpsKey = "")
	{
		if (string.IsNullOrWhiteSpace(address))
		{
			throw new ArgumentNullException("address");
		}
		if (port < 1)
		{
			throw new ArgumentException("post must be > 0");
		}
		_tx = new ShipTransmitter(address, port, !useHttps, basicUsername, basicPassword, saltoShipHttpsKey);
		Info = new ApiInfo(_tx);
		SaltoDb = new SaltoDb_OnlineDoors(_tx);
		Encoder = new Encoder(_tx);
	}
}
