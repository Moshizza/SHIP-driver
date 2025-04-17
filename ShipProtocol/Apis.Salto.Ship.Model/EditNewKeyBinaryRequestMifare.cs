namespace Apis.Salto.Ship.Model;

public class EditNewKeyBinaryRequestMifare : EditNewKeyBinaryRequest
{
	public MifareStructure MifareStructure { get; set; } = new MifareStructure();

}
