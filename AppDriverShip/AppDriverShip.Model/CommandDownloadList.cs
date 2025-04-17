using System.Collections.Generic;
using Prysm.AppControl.SDK.AppVisionHistoDataServiceReference;

namespace AppDriverShip.Model;

internal class CommandDownloadList
{
	public List<int> Users = new List<int>();

	public List<int> Timetables = new List<int>();

	public List<int> Variables = new List<int>();

	public List<ModifRow> Modifs = new List<ModifRow>();

	public bool Holidays;

	public bool Parameters;
}
