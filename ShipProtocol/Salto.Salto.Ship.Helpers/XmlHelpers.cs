using System;
using Prysm.AppVision.Common;
using Prysm.AppVision.SDK;

namespace Salto.Salto.Ship.Helpers;

internal static class XmlHelpers
{
	internal static string FindIdMarkup(string xml)
	{
		if (string.IsNullOrWhiteSpace(xml))
		{
			return "";
		}
		if (!xml.Contains("ID"))
		{
			return "";
		}
		string text = xml.Substring(xml.IndexOf("ID", StringComparison.InvariantCulture));
		if (!text.Contains("</"))
		{
			return "";
		}
		string text2 = text.Substring(text.IndexOf("</", StringComparison.InvariantCulture) + 2);
		if (!text2.Contains(">"))
		{
			return "";
		}
		return text2.Substring(0, text2.IndexOf(">", StringComparison.InvariantCulture));
	}

	internal static string ResponseReadToXmlObject(string xmlResponse, string objectName)
	{
		if (string.IsNullOrWhiteSpace(xmlResponse))
		{
			return "";
		}
		if (string.IsNullOrWhiteSpace(objectName))
		{
			return "";
		}
		string text = objectName ?? "";
		if (xmlResponse.Contains("<Exception>") && xmlResponse.Contains("<Code>") && xmlResponse.Contains("<Message>") && xmlResponse.Contains("<RequestName>"))
		{
			string text2 = StringExtensions.Between(xmlResponse, "<Code>", "</Code>");
			string text3 = StringExtensions.Between(xmlResponse, "<Message>", "</Message>");
			string text4 = StringExtensions.Between(xmlResponse, "<RequestName>", "</RequestName>");
			Trace.Warning("ReadResponseToXmlObject request " + text4 + " ERROR code=" + text2 + " > " + text3, "ResponseReadToXmlObject", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Helpers\\XmlHelpers.cs", 44);
			return "";
		}
		if (xmlResponse.ToLower().Contains("<" + text.ToLower() + "/>"))
		{
			return "";
		}
		string text5 = StringExtensions.Between(xmlResponse, "<" + text + ">", "</" + text + ">");
		if (text.EndsWith("List", StringComparison.InvariantCulture))
		{
			objectName = objectName.Remove(objectName.Length - "List".Length);
			if (string.IsNullOrWhiteSpace(text5))
			{
				Trace.Warning("ReadResponseToXmlObject Key not found: " + text + " in " + xmlResponse, "ResponseReadToXmlObject", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Helpers\\XmlHelpers.cs", 59);
				return "";
			}
		}
		return "<?xml version=\"1.0\" encoding=\"utf-8\" ?><ArrayOf" + objectName + ">" + text5 + "</ArrayOf" + objectName + ">";
	}
}
