namespace Apis.Salto.Ship.Model;

public class SaltoDBTimezone
{
	public int TimezoneTableID { get; set; }

	public int TimezoneID { get; set; }

	public string StartTime { get; set; }

	public string EndTime { get; set; }

	public int Monday { get; set; }

	public int Tuesday { get; set; }

	public int Wednesday { get; set; }

	public int Thursday { get; set; }

	public int Friday { get; set; }

	public int Saturday { get; set; }

	public int Sunday { get; set; }

	public int Special1 { get; set; }

	public int Special2 { get; set; }

	public int Holiday { get; set; }

	public override string ToString()
	{
		return $"SaltoDBTimezoneTable: TimezoneTableID={TimezoneTableID} TimezoneID={TimezoneID} StartTime={StartTime} EndTime={EndTime} ";
	}
}
