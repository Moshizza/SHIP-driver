using System.Collections.Generic;
using System.Text;

namespace Apis.Salto.Ship.Model;

public class SaltoDBTimezoneTable
{
	public int TimezoneTableID { get; set; }

	public string Name { get; set; }

	public string Description { get; set; }

	public List<SaltoDBTimezone> SaltoDBTimezoneList { get; set; } = new List<SaltoDBTimezone>();


	public override string ToString()
	{
		string value = "";
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(value);
		foreach (SaltoDBTimezone saltoDBTimezone in SaltoDBTimezoneList)
		{
			stringBuilder.Append(saltoDBTimezone.ToString());
		}
		value = stringBuilder.ToString();
		return $"SaltoDBTimezoneTable: TimezoneTableID={TimezoneTableID} Name={Name} Description={Description} SaltoDBTimezoneList={value} ";
	}
}
