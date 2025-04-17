namespace Apis.Salto.Ship.Model;

public class EditNewKeyBinaryResponseMifare : EditNewKeyBinaryResponse
{
	public MifareBinaryData MifareBinaryData { get; set; } = new MifareBinaryData();

}
