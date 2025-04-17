namespace Apis.Salto.Ship.Model;

public class Timezone
{
	public string StartTime { get; set; }

	public string EndTime { get; set; }

	public string DayType { get; set; }

	public override string ToString()
	{
		return "Timezone: StartTime=" + StartTime + " EndTime=" + EndTime + " DayType=" + DayType;
	}
}
