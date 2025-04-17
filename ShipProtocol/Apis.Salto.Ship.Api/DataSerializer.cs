using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Prysm.AppVision.SDK;

namespace Apis.Salto.Ship.Api;

public static class DataSerializer<T> where T : class
{
	public static string Serializer(T o)
	{
		string text = "";
		XmlSerializer xmlSerializer = new XmlSerializer(o.GetType());
		XmlWriterSettings settings = new XmlWriterSettings
		{
			Encoding = Encoding.UTF8,
			Indent = true,
			OmitXmlDeclaration = true
		};
		using (StringWriter stringWriter = new StringWriter())
		{
			using XmlWriter xmlWriter = XmlWriter.Create(stringWriter, settings);
			xmlSerializer.Serialize(xmlWriter, o);
			text = stringWriter.ToString();
		}
		string text2 = "<" + o.GetType().Name;
		if (text.Contains(text2))
		{
			text = text.Substring(text.IndexOf(text2, StringComparison.InvariantCulture));
			text = text.Substring(text.IndexOf(">", StringComparison.InvariantCulture) + 1);
			text = text2 + ">" + text;
		}
		return text;
	}

	public static T[] DeserializeReadResponseArray(string xmlObject)
	{
		if (string.IsNullOrWhiteSpace(xmlObject))
		{
			throw new ArgumentNullException(xmlObject);
		}
		try
		{
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(T[]));
			using StringReader textReader = new StringReader(xmlObject);
			return (T[])xmlSerializer.Deserialize(textReader);
		}
		catch (Exception)
		{
			throw;
		}
	}

	public static T DeserializeReadResponse(string xmlObject)
	{
		if (string.IsNullOrWhiteSpace(xmlObject))
		{
			throw new ArgumentNullException(xmlObject);
		}
		try
		{
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
			using StringReader textReader = new StringReader(xmlObject);
			return (T)xmlSerializer.Deserialize(textReader);
		}
		catch (Exception ex)
		{
			Trace.Error("DeserializeReadResponse error. Src object: " + xmlObject, "DeserializeReadResponse", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\~DataSerializer.cs", 78);
			Trace.Error(ex, null, "DeserializeReadResponse", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Api\\~DataSerializer.cs", 79);
			throw;
		}
	}
}
