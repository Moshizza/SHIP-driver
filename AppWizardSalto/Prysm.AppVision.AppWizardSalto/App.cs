#define TRACE
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using Prysm.AppVision.Common;

namespace Prysm.AppVision.AppWizardSalto;

public partial class App : Application
{
	public App()
	{
		AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
	}

	private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs a)
	{
		try
		{
			if (StringExtensions.StartsWithInv(a.Name, new string[1] { "Telerik." }))
			{
				string text = Path.Combine(Helper.PathBin, StringExtensions.Suffix(new AssemblyName(a.Name).Name, ".dll"));
				Trace.WriteLine("looking for " + text);
				if (File.Exists(text))
				{
					return Assembly.LoadFrom(text);
				}
			}
		}
		catch
		{
		}
		return null;
	}
}
