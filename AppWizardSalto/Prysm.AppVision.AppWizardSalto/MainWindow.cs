using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using Prysm.AppVision.AppWizardSalto.Utils;
using Prysm.AppVision.AppWizardSalto.ViewModels;
using Prysm.AppVision.Common;

namespace Prysm.AppVision.AppWizardSalto;

public partial class MainWindow : Window, IComponentConnector
{
	public MainWindow()
	{
		try
		{
			RT.Load(Path.Combine(Helper.PathResources, "AppLab.txt"));
		}
		catch
		{
		}
		InitializeComponent();
		base.Closed += MainWindow_Closed;
		ViewModel.Frame = MainFrame;
		MainFrame.Navigate(new Uri("Views/Welcome.xaml", UriKind.Relative));
	}

	private void MainWindow_Closed(object sender, EventArgs e)
	{
		try
		{
			AppVisionComm.Server.Close();
		}
		catch
		{
		}
	}
}
