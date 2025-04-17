using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Prysm.AppVision.AppWizardSalto.Views;

public partial class Complete : Page, IComponentConnector
{
	public Complete()
	{
		InitializeComponent();
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		Application.Current.Shutdown(1);
	}
}
