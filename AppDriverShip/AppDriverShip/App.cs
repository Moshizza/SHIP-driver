using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Prysm.AppVision.Common;
using Prysm.AppVision.SDK;

namespace AppDriverShip;

public class App : Application
{
	private Driver _driver;

	private Window _window;

	protected override void OnStartup(StartupEventArgs e)
	{
		AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs ex)
		{
			UnhandledException((Exception)ex.ExceptionObject);
		};
		base.DispatcherUnhandledException += delegate(object s, DispatcherUnhandledExceptionEventArgs ex)
		{
			UnhandledException(ex.Exception);
		};
		if (Environment.GetCommandLineArgs().Length < 2)
		{
			MessageBox.Show("Usage: " + Assembly.GetEntryAssembly().GetName().Name + " protocolName[@hostName]", Helper.ProductName, MessageBoxButton.OK, MessageBoxImage.Asterisk);
			Shutdown(1);
			return;
		}
		_window = new MainWindow();
		_window.Closing += Window_Closing;
		_window.CommandBindings.Add(new CommandBinding(NavigationCommands.Refresh, delegate
		{
			_driver.Refresh("");
		}));
		_window.CommandBindings.Add(new CommandBinding(ApplicationCommands.CorrectionList, delegate
		{
			_driver.Download("", "");
		}));
		_window.Show();
		_driver = new Driver();
		((AppServer)_driver.AppServer).ControllerManager.Closed += Driver_Closing;
		_driver.Start();
	}

	private void Driver_Closing()
	{
		Prysm.AppVision.SDK.Trace.Info("Stopping asked", "Driver_Closing", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\App.xaml.cs", 52);
		_driver.Stop();
		_window.Closing -= Window_Closing;
		_window.Close();
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
		{
			MessageBox.Show(RT.Get("No_user_stop"), Helper.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
			e.Cancel = true;
		}
		else
		{
			_driver?.Stop();
		}
	}

	private void UnhandledException(Exception ex)
	{
		Prysm.AppVision.SDK.Trace.Error(ex, null, "UnhandledException", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\App.xaml.cs", 71);
		Shutdown();
		MessageBox.Show(ex?.Message ?? "Fatal error", Helper.ProductName, MessageBoxButton.OK, MessageBoxImage.Hand);
	}

	private new async void Shutdown()
	{
		Prysm.AppVision.SDK.Trace.Error("Closing driver in 5 seconds...", "Shutdown", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\App.xaml.cs", 78);
		await Task.Delay(5000);
		Driver_Closing();
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public static void Main()
	{
		new App().Run();
	}
}
