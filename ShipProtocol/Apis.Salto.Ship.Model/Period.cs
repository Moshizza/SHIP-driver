namespace Apis.Salto.Ship.Model;

public class Period
{
	public string StartDate { get; set; }

	public string EndDate { get; set; }

	public override string ToString()
	{
		return "Period: StartDate=" + StartDate + " EndDate=" + EndDate;
	}
}
