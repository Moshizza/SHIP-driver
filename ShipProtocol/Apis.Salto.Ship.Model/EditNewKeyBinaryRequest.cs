using System.Xml.Serialization;

namespace Apis.Salto.Ship.Model;

[XmlInclude(typeof(EditNewKeyBinaryRequestDesfire))]
public abstract class EditNewKeyBinaryRequest
{
	public string KeyID { get; set; }

	public string KeyToReplace { get; set; }

	public SaltoKeyData SaltoKeyData { get; set; }

	public string ExtAppList { get; set; }

	public int KeyIsCancellableThroughBlacklist { get; set; } = 1;


	public int ReturnKeyID { get; set; } = 1;

}
