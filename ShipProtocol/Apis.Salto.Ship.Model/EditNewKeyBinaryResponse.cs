using System.Xml.Serialization;

namespace Apis.Salto.Ship.Model;

[XmlInclude(typeof(EditNewKeyBinaryResponseDesfire))]
[XmlInclude(typeof(EditNewKeyBinaryResponseMifare))]
public abstract class EditNewKeyBinaryResponse
{
	public string KeyID { get; set; }
}
