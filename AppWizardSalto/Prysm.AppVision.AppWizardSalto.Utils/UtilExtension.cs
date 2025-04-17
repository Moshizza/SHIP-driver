using Prysm.AppVision.Common;
using Unidecode.NET;

namespace Prysm.AppVision.AppWizardSalto.Utils;

public static class UtilExtension
{
	public static string GetStrongVariableName(this string name)
	{
		return StringExtensions.ToAlphaNum(name.Unidecode(), "");
	}
}
