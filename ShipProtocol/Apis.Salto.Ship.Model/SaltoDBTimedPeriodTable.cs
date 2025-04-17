using System.Collections.Generic;
using System.Text;

namespace Apis.Salto.Ship.Model;

public class SaltoDBTimedPeriodTable
{
	public int TimedPeriodTableID { get; set; }

	public string Name { get; set; }

	public string Description { get; set; }

	public List<SaltoDBTimedPeriod> SaltoDBTimedPeriodList { get; set; } = new List<SaltoDBTimedPeriod>();


	public override string ToString()
	{
		string value = "";
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(value);
		foreach (SaltoDBTimedPeriod saltoDBTimedPeriod in SaltoDBTimedPeriodList)
		{
			stringBuilder.Append(saltoDBTimedPeriod.ToString());
		}
		value = stringBuilder.ToString();
		return $"SaltoDBTimezoneTable: TimezoneTableID={TimedPeriodTableID} Name={Name} Description={Description} SaltoDBTimezoneList={value} ";
	}
}
