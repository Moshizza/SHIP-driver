using System;
using System.Threading.Tasks;
using Prysm.AppVision.Data;
using Prysm.AppVision.SDK;

namespace Prysm.AppVision.AppWizardSalto.Utils;

internal static class AppVisionComm
{
	private static VariableRow[] _saltoServers;

	public static AppServer Server { get; private set; }

	public static async Task<bool> ConnectAsync(string address, string username, string password)
	{
		try
		{
			if (Server == null)
			{
				Server = new AppServer();
			}
			else if (Server.IsConnected)
			{
				await Task.Run(delegate
				{
					Server.Close();
				});
			}
			await Task.Run(delegate
			{
				Server.Open(address);
			});
			await Task.Run(delegate
			{
				Server.Login(username.ToUpper(), password);
			});
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static VariableRow[] GetSystemServers()
	{
		return _saltoServers ?? (_saltoServers = Server.VariableManager.GetRowsByFilter(new string[2] { "$V.Salto*", "Type=Node" }));
	}
}
