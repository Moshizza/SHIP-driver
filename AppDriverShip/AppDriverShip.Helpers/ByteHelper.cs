using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace AppDriverShip.Helpers;

internal static class ByteHelper
{
	internal static bool[] ToBoolArray(this long source)
	{
		byte[] bytes = BitConverter.GetBytes(source);
		bool[] array = new bool[bytes.Length];
		for (int i = 0; i < bytes.Length; i++)
		{
			array[i] = bytes[i] == 1;
		}
		return array;
	}

	internal static bool[] ToBoolArray(this int source)
	{
		return ((long)source).ToBoolArray();
	}

	internal static byte ToByte(this bool[] source)
	{
		BitArray bitArray = new BitArray(source);
		byte[] array = new byte[1];
		bitArray.CopyTo(array, 0);
		return array[0];
	}

	internal static byte[] ToByteArray(this bool[] source)
	{
		int i = source.Length / 8;
		if (source.Length % 8 > 0)
		{
			i++;
		}
		for (; i % 2 != 0; i++)
		{
		}
		BitArray bitArray = new BitArray(source);
		byte[] array = new byte[i];
		bitArray.CopyTo(array, 0);
		return array;
	}

	internal static long ToLong(this byte[] source)
	{
		if (source.Length > 8)
		{
			throw new ArgumentOutOfRangeException("source");
		}
		byte[] array = new byte[8];
		if (source.Length == 8)
		{
			array = source;
		}
		else
		{
			source.CopyTo(array, 0);
		}
		return BitConverter.ToInt64(array, 0);
	}

	internal static long ToLong(this bool[] source)
	{
		return source.ToByteArray().ToLong();
	}

	internal static bool GetBit(this byte b, int bitNumber)
	{
		if (bitNumber > 7)
		{
			bitNumber = 7;
		}
		if (bitNumber < 0)
		{
			bitNumber = 0;
		}
		return new BitArray(new byte[1] { b })[bitNumber];
	}

	internal static string ByteToHexString(byte[] bytes)
	{
		return string.Concat(bytes.Select((byte b) => b.ToString("X2")).ToArray());
	}

	internal static byte[] HexStringToBytes(this string hexEncodedBytes, int start, int end)
	{
		int num = end - start;
		string s = string.Format("<{1}>{0}</{1}>", hexEncodedBytes.Substring(start, num), "hex");
		XmlReader xmlReader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(s)), new XmlReaderSettings());
		int num2 = num / 2;
		byte[] array = new byte[num2];
		xmlReader.ReadStartElement("hex");
		xmlReader.ReadContentAsBinHex(array, 0, num2);
		return array;
	}
}
