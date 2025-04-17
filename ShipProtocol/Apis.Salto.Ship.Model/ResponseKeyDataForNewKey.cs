namespace Apis.Salto.Ship.Model;

public class ResponseKeyDataForNewKey
{
	public string ROMCode { get; set; }

	public string KeyID { get; set; }

	public string KeyToReplace { get; set; }

	public ActionToDo ActionsToDo { get; set; } = new ActionToDo();


	public SaltoKeyData SaltoKeyData { get; set; } = new SaltoKeyData();

}
