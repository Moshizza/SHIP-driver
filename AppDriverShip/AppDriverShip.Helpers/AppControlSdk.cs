using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Text;
using Prysm.AppControl.SDK.AppControlDataServiceReference;
using Prysm.AppVision.SDK;

namespace AppDriverShip.Helpers;

internal class AppControlSdk
{
	private AppControlContext _context;

	private string username = "";

	private string password = "";

	private string _base64Creds = "";

	private Driver _driver;

	public AppControlSdk(Driver driver)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		_driver = driver;
		_context = new AppControlContext(new Uri(((AppServer)driver.AppServer).Hostname + "AppControlDataService.svc/"));
		((DataServiceContext)_context).MergeOption = (MergeOption)1;
		username = "$P." + driver.AppServer.CurrentProtocol.Name;
		password = ((AppServer)driver.AppServer).SessionId;
		string s = username + ":" + password;
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		_base64Creds = Convert.ToBase64String(bytes);
		((DataServiceContext)_context).SendingRequest2 += _context_SendingRequest2;
	}

	private void _context_SendingRequest2(object sender, SendingRequest2EventArgs e)
	{
		e.RequestMessage.SetHeader("Authorization", "Basic " + _base64Creds);
		e.RequestMessage.SetHeader("LocalId", Process.GetCurrentProcess().Id.ToString());
	}

	public void Save()
	{
		((DataServiceContext)_context).SaveChanges();
	}

	public DataServiceCollection<Badge> GetBadges()
	{
		return new DataServiceCollection<Badge>((IEnumerable<Badge>)_context.Badges);
	}
}
