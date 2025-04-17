using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Microsoft.Win32;
using Prysm.AppVision.AppWizardSalto.Utils;
using Prysm.AppVision.AppWizardSalto.ViewModels;
using Prysm.AppVision.Common;

namespace Prysm.AppVision.AppWizardSalto.Views;

public partial class Symbols : Page, IComponentConnector
{
	private ViewModel Vm => (ViewModel)base.DataContext;

	public Symbols()
	{
		InitializeComponent();
	}

	private void Offline_Click(object sender, RoutedEventArgs e)
	{
		ViewModel viewModel = base.DataContext as ViewModel;
		string text = Path.Combine(AppVisionComm.Server.GetPathConfig(), "Param\\Client");
		string initialDirectory = Path.Combine(text, Path.GetDirectoryName(viewModel.OfflineDoorSymbol));
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "Symbols files (*.xaml)|*.xaml";
		openFileDialog.InitialDirectory = initialDirectory;
		if (openFileDialog.ShowDialog() != true)
		{
			return;
		}
		string text2 = openFileDialog.FileName;
		if (!string.IsNullOrEmpty(text2))
		{
			if (text2.ToLower().StartsWith(text.ToLower()))
			{
				text2 = text2.Substring(text.Length + 1);
			}
			viewModel.OfflineDoorSymbol = text2;
		}
	}

	private void Ip_Click(object sender, RoutedEventArgs e)
	{
		ViewModel viewModel = base.DataContext as ViewModel;
		string text = Path.Combine(AppVisionComm.Server.GetPathConfig(), "Param\\Client");
		string initialDirectory = Path.Combine(text, Path.GetDirectoryName(viewModel.IpDoorSymbol));
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "Symbols files (*.xaml)|*.xaml";
		openFileDialog.InitialDirectory = initialDirectory;
		if (openFileDialog.ShowDialog() != true)
		{
			return;
		}
		string text2 = openFileDialog.FileNames?.FirstOrDefault();
		if (!string.IsNullOrEmpty(text2))
		{
			if (text2.ToLower().StartsWith(text.ToLower()))
			{
				text2 = text2.Substring(text.Length + 1);
			}
			viewModel.IpDoorSymbol = text2;
		}
	}

	private void Rf_Click(object sender, RoutedEventArgs e)
	{
		ViewModel viewModel = base.DataContext as ViewModel;
		string text = Path.Combine(AppVisionComm.Server.GetPathConfig(), "Param\\Client");
		string initialDirectory = Path.Combine(text, Path.GetDirectoryName(viewModel.RFDoorSymbol));
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "Symbols files (*.xaml)|*.xaml";
		openFileDialog.InitialDirectory = initialDirectory;
		if (openFileDialog.ShowDialog() != true)
		{
			return;
		}
		string text2 = openFileDialog.FileNames?.FirstOrDefault();
		if (!string.IsNullOrEmpty(text2))
		{
			if (text2.ToLower().StartsWith(text.ToLower()))
			{
				text2 = text2.Substring(text.Length + 1);
			}
			viewModel.RFDoorSymbol = text2;
		}
	}

	private void Locker_Click(object sender, RoutedEventArgs e)
	{
		Vm.LockerSymbol = GetSymbolPath(Vm.LockerSymbol);
	}

	private void HotelRoom_Click(object sender, RoutedEventArgs e)
	{
		Vm.HotelRoomSymbol = GetSymbolPath(Vm.HotelRoomSymbol);
	}

	private void HotelSuite_Click(object sender, RoutedEventArgs e)
	{
		Vm.HotelSuiteSymbol = GetSymbolPath(Vm.HotelSuiteSymbol);
	}

	private string GetSymbolPath(string currentSymbol)
	{
		string text = Path.Combine(AppVisionComm.Server.GetPathConfig(), "Param\\Client\\");
		string initialDirectory = Path.Combine(text, Path.GetDirectoryName(currentSymbol));
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "Symbols files (*.xaml)|*.xaml";
		openFileDialog.InitialDirectory = initialDirectory;
		if (openFileDialog.ShowDialog() == true)
		{
			return StringExtensions.TrimStart(openFileDialog.FileName, new string[1] { text });
		}
		return null;
	}
}
