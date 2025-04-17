using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using AppDriverShip.Helpers;
using AppDriverShip.Properties;
using Microsoft.Win32;
using Prysm.AppVision.Common;
using Prysm.AppVision.SDK;

namespace AppDriverShip;

public partial class MainWindow : Window, IComponentConnector
{
	private ConcurrentQueue<LogEntry> _buffer = new ConcurrentQueue<LogEntry>();

	public static readonly DependencyProperty ScrollLockProperty = DependencyProperty.Register("ScrollLock", typeof(bool), typeof(MainWindow));

	public int MaxCount { get; set; } = Settings.Default.MaxCount;


	public LogLevel LogLevel { get; set; } = (LogLevel)Settings.Default.LogLevel;


	public ObservableCollection<LogEntry> Items { get; } = new ObservableCollection<LogEntry>();


	public bool ScrollLock
	{
		get
		{
			return (bool)GetValue(ScrollLockProperty);
		}
		set
		{
			SetValue(ScrollLockProperty, value);
		}
	}

	public MainWindow()
	{
		Trace.Logger.LogEvent += NewLog;
		Trace.Info(Helper.StartupInfo(), ".ctor", "C:\\devops\\agent1\\_work\\54\\s\\DriverShip\\MainWindow.xaml.cs", 32);
		base.Title = $"{Helper.ProductName} v{Helper.ProductVersion} - {Environment.GetCommandLineArgs()[1]} from {Assembly.GetEntryAssembly().GetBuildDateTime()}";
		try
		{
			RT.Load(Path.Combine(Helper.PathResources, "AppLab.txt"));
		}
		catch
		{
		}
		base.DataContext = this;
		InitializeComponent();
		DispatcherTimer dispatcherTimer = new DispatcherTimer();
		dispatcherTimer.Interval = TimeSpan.FromSeconds(1.0);
		dispatcherTimer.IsEnabled = true;
		dispatcherTimer.Tick += Tick;
		CollectionViewSource.GetDefaultView(Items).Filter = Items_Filter;
		level.ItemsSource = Enum.GetValues(typeof(LogLevel));
	}

	private void Window_Closed(object sender, EventArgs e)
	{
		Settings.Default.MaxCount = MaxCount;
		Settings.Default.LogLevel = (int)LogLevel;
		Settings.Default.Save();
	}

	private void NewLog(LogEntry it)
	{
		if (LogLevel <= it.Level && _buffer.Count < 1000)
		{
			_buffer.Enqueue(it);
		}
	}

	private void Tick(object sender, EventArgs e)
	{
		if (Items.Count > MaxCount)
		{
			for (int i = 0; i < MaxCount / 10; i++)
			{
				Items.RemoveAt(0);
			}
		}
		if (_buffer.Count > 0)
		{
			ConcurrentQueue<LogEntry> buffer = _buffer;
			_buffer = new ConcurrentQueue<LogEntry>();
			CollectionExtensions.AddRange<LogEntry>((ICollection<LogEntry>)Items, (IEnumerable<LogEntry>)buffer);
		}
		if (!ScrollLock)
		{
			DependencyObjectExtensions.GetChild<ScrollViewer>((DependencyObject)logs, (string)null)?.ScrollToBottom();
		}
		statusInfo.Text = Trace.StatusText;
	}

	private bool Items_Filter(object item)
	{
		if (!string.IsNullOrEmpty(filter.Text) && ((LogEntry)item).Message.IndexOf(filter.Text, StringComparison.CurrentCultureIgnoreCase) == -1)
		{
			return false;
		}
		return true;
	}

	private void Filter_Changed(object sender, TextChangedEventArgs e)
	{
		CollectionViewSource.GetDefaultView(Items).Refresh();
	}

	private void ClearFilter_Click(object sender, RoutedEventArgs e)
	{
		filter.Clear();
	}

	private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		WaitCursor val = new WaitCursor();
		try
		{
			if (logs.SelectedItem == null)
			{
				Clipboard.SetText(CollectionExtensions.Join<LogEntry>((IEnumerable<LogEntry>)Items, ","));
			}
			else
			{
				Clipboard.SetText(CollectionExtensions.Join<LogEntry>(logs.SelectedItems.Cast<LogEntry>(), ","));
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private void Clear_Executed(object sender, ExecutedRoutedEventArgs e)
	{
		Items.Clear();
	}

	private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		string name = Assembly.GetEntryAssembly().GetName().Name;
		saveFileDialog.FileName = $"{name}_{DateTime.Now:yyMMdd_HHmm}.log";
		if (saveFileDialog.ShowDialog() != true)
		{
			return;
		}
		WaitCursor val = new WaitCursor();
		try
		{
			using StreamWriter streamWriter = new StreamWriter(saveFileDialog.FileName);
			foreach (LogEntry item in Items)
			{
				streamWriter.WriteLine(item);
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private void ToolBar_Loaded(object sender, RoutedEventArgs e)
	{
		ToolBar toolBar = sender as ToolBar;
		if (toolBar.Template.FindName("OverflowGrid", toolBar) is FrameworkElement frameworkElement)
		{
			frameworkElement.Visibility = Visibility.Collapsed;
		}
	}

	private void Tools_Click(object sender, RoutedEventArgs e)
	{
		toolsPopup.IsOpen = true;
	}
}
