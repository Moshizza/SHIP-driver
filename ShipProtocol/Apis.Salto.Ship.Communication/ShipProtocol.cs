using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Prysm.AppVision.SDK;

namespace Apis.Salto.Ship.Communication;

internal class ShipProtocol
{
	private byte[] GenerateFrame(string saltoFrame)
	{
		string s = $"STP/00/{saltoFrame.Length}/" + saltoFrame;
		return Encoding.UTF8.GetBytes(s);
	}

	internal async Task<bool> SendFrame(NetworkStream stream, string frame)
	{
		if (string.IsNullOrWhiteSpace(frame))
		{
			return false;
		}
		if (stream == null)
		{
			return false;
		}
		if (!stream.CanWrite)
		{
			return false;
		}
		try
		{
			byte[] array = GenerateFrame(frame);
			await stream.WriteAsync(array, 0, array.Length);
			return true;
		}
		catch (Exception ex)
		{
			Trace.Error(ex, null, "SendFrame", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipProtocol.cs", 41);
			return false;
		}
	}

	internal string ReceiveShipResponse(NetworkStream stream, string method = "")
	{
		try
		{
			if (stream.CanRead)
			{
				StringBuilder stringBuilder = new StringBuilder();
				int num;
				int size;
				do
				{
					byte[] array = new byte[4096];
					num = 0;
					using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
					do
					{
						Thread.Sleep(200);
						if (!stream.CanRead)
						{
							return stringBuilder?.ToString();
						}
						streamReader.BaseStream.ReadTimeout = 5000;
						int num2 = stream.Read(array, 0, array.Length);
						num += num2;
						string @string = Encoding.UTF8.GetString(array, 0, num2);
						stringBuilder.AppendFormat(@string);
					}
					while (stream.DataAvailable);
				}
				while (TryGetSize(stringBuilder.ToString(), out size) && num < size);
				return GetValidFrameContent(stringBuilder.ToString());
			}
			return "ERROR - ShipProtocol.ReceiveShipResponse Can not read stream";
		}
		catch (Exception ex)
		{
			Trace.Error($"ShipProtocol.ReceiveShipResponse ex {method} > {ex}", "ReceiveShipResponse", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipProtocol.cs", 97);
			return "ERROR - ShipProtocol.ReceiveShipResponse ex: " + ex.Message;
		}
	}

	private bool TryGetSize(string frame, out int size)
	{
		size = -1;
		if (!frame.Contains("<"))
		{
			return false;
		}
		string text = frame.Remove(frame.IndexOf("<") - 1);
		if (!text.ToUpper().StartsWith("STP"))
		{
			return false;
		}
		if (!text.Contains("/"))
		{
			return false;
		}
		return int.TryParse(text.Substring(text.LastIndexOf("/") + 1), out size);
	}

	private string GetValidFrameContent(string fullFrame)
	{
		if (string.IsNullOrWhiteSpace(fullFrame))
		{
			Trace.Debug("ShipProtocol.GetValidFrameContent - empty frame", "GetValidFrameContent", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipProtocol.cs", 120);
			return "";
		}
		if (!fullFrame.StartsWith("STP/00/"))
		{
			Trace.Debug("ShipProtocol.GetValidFrameContent - invalid frame header", "GetValidFrameContent", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipProtocol.cs", 126);
			return "";
		}
		string text = fullFrame.Substring(7);
		if (!text.Contains("/"))
		{
			Trace.Debug("ShipProtocol.GetValidFrameContent - invalid frame", "GetValidFrameContent", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipProtocol.cs", 133);
			return "";
		}
		int result = 0;
		if (!int.TryParse(text.Substring(0, text.IndexOf("/")), out result))
		{
			Trace.Debug("ShipProtocol.GetValidFrameContent - invalid frame header count", "GetValidFrameContent", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipProtocol.cs", 141);
			return "";
		}
		string text2 = text.Substring(text.IndexOf("/") + 1);
		if (!text.ToLower().Contains("utf-8") && text2.Length != result)
		{
			Trace.Debug("ShipProtocol.GetValidFrameContent - invalid frame count", "GetValidFrameContent", "C:\\devops\\agent1\\_work\\54\\s\\ShipProtocol\\Salto\\Ship\\Communication\\ShipProtocol.cs", 150);
			return "";
		}
		return text2;
	}
}
