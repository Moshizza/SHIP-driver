using System;

namespace Prysm.AppVision.SDK.Logger;

[AttributeUsage(AttributeTargets.All)]
public class LoggerAttribute : Attribute
{
	public bool IgnoreInTree;

	public LoggerAttribute()
	{
		IgnoreInTree = true;
	}

	public LoggerAttribute(bool ignoreInTree)
	{
		IgnoreInTree = ignoreInTree;
	}
}
