using System.Text;

namespace Apis.Salto.Ship.Api;

internal class SaltoFormatter
{
	internal string GenerateFrame(string method, string parameters)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
		stringBuilder.AppendLine("<RequestCall>");
		stringBuilder.AppendLine("<RequestName>");
		stringBuilder.AppendLine(method);
		stringBuilder.AppendLine("</RequestName>");
		if (string.IsNullOrEmpty(parameters))
		{
			stringBuilder.AppendLine("<Params/>");
		}
		else
		{
			stringBuilder.AppendLine("<Params>");
			stringBuilder.AppendLine(parameters);
			stringBuilder.AppendLine("</Params>");
		}
		stringBuilder.Append("</RequestCall>");
		return stringBuilder.ToString();
	}
}
