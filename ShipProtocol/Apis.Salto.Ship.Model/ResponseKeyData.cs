namespace Apis.Salto.Ship.Model;

public class ResponseKeyData
{
	public string KeyID { get; set; }

	public ActionToDo ActionsToDo { get; set; } = new ActionToDo();


	public SaltoKeyData SaltoKeyData { get; set; } = new SaltoKeyData();

}
