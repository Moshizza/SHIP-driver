namespace Apis.Salto.Ship.Model;

public class Info
{
	public string ProtocolID { get; set; }

	public string ProtocolVersion { get; set; }

	public string DateTime { get; set; }

	public string DefaultLanguage { get; set; }

	public override string ToString()
	{
		return "Info: ProtocolID=" + ProtocolID + " ProtocolVersion=" + ProtocolVersion + " DateTime=" + DateTime + " DefaultLanguage=" + DefaultLanguage;
	}
}
