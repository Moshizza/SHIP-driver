namespace Apis.Salto.Ship.Model;

public class EditNewKeyRequest
{
	public string EncoderID { get; set; }

	public string KeyID { get; set; }

	public string KeyToReplace { get; set; }

	public SaltoKeyData SaltoKeyData { get; set; }

	public int Timeout { get; set; } = 20;


	public int KeyIsCancellableThroughBlacklist { get; set; } = 1;


	public int ReturnKeyID { get; set; } = 1;


	public int ReturnROMCode { get; set; } = 1;


	public int SkipKeyRemoval { get; set; } = 1;

}
