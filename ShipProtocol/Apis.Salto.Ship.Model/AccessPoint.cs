namespace Apis.Salto.Ship.Model;

public class AccessPoint
{
	public int AccessType { get; set; }

	public string AccessID { get; set; }

	public override string ToString()
	{
		return $"AccessPoint: AccessType={AccessType} AccessID={AccessID}";
	}
}
