using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AppDriverShip.Helpers;
using Prysm.AppVision.SDK;

namespace AppDriverShip.Business;

internal abstract class BaseByteEnumVariable
{
	public abstract string[] GetPropertiesNamesInByteEnum();

	public int GetPropertiesAsByteEnumValue()
	{
		try
		{
			return (int)GetPropsTable().ToLong();
		}
		catch (Exception ex)
		{
			Trace.Debug("BaseByteEnumVariable exception " + ex.Message, "GetPropertiesAsByteEnumValue", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\BaseByteEnumVariable.cs", 25);
			Trace.Error(ex, null, "GetPropertiesAsByteEnumValue", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\BaseByteEnumVariable.cs", 26);
			return -1;
		}
	}

	public void SetPropertiesFromByteEnumValue(int value)
	{
		BitArray bitArray = new BitArray(BitConverter.GetBytes(value));
		string[] propertiesNamesInByteEnum = GetPropertiesNamesInByteEnum();
		for (int i = 0; i < propertiesNamesInByteEnum.Length; i++)
		{
			if (bitArray.Length > i)
			{
				SetPropertyByName(propertiesNamesInByteEnum[i], bitArray[i]);
			}
		}
	}

	private void SetPropertyByName(string propName, bool value)
	{
		try
		{
			PropertyInfo property = GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
			if (null != property && property.CanWrite)
			{
				property.SetValue(this, value, null);
			}
		}
		catch (Exception arg)
		{
			Trace.Error($"Door.SetPropertyByName exception for {propName} >>> {arg}", "SetPropertyByName", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\BaseByteEnumVariable.cs", 58);
		}
	}

	private bool[] GetPropsTable()
	{
		try
		{
			string[] propertiesNamesInByteEnum = GetPropertiesNamesInByteEnum();
			List<bool> list = new List<bool>();
			bool flag = false;
			string[] array = propertiesNamesInByteEnum;
			foreach (string propName in array)
			{
				if (TryGetValueByPropertyName(propName, out var result))
				{
					list.Add(result);
				}
				else
				{
					flag = true;
				}
			}
			if (!flag)
			{
				return list.ToArray();
			}
		}
		catch (Exception ex)
		{
			Trace.Debug("GetPropsTable Exception " + ex.Message, "GetPropsTable", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\BaseByteEnumVariable.cs", 83);
			Trace.Error(ex, null, "GetPropsTable", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\BaseByteEnumVariable.cs", 84);
		}
		Trace.Error("GetPropsTable had errors", "GetPropsTable", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\BaseByteEnumVariable.cs", 86);
		return new bool[0];
	}

	private bool TryGetValueByPropertyName(string propName, out bool result)
	{
		result = false;
		try
		{
			PropertyInfo property = GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
			if (null != property && property.CanRead)
			{
				result = (bool)property.GetValue(this);
				return true;
			}
		}
		catch (Exception arg)
		{
			Trace.Error($"TryGetValueByPropertyName exception for {propName} >>> {arg}", "TryGetValueByPropertyName", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\Business\\BaseByteEnumVariable.cs", 105);
		}
		return false;
	}
}
