using System.Windows.Controls;
using System.Windows.Markup;

namespace Prysm.AppVision.AppWizardSalto.Views;

public partial class Summary : Page, IComponentConnector
{
	public Summary()
	{
		InitializeComponent();
	}

	private void BringSelectionIntoView(object sender, SelectionChangedEventArgs e)
	{
		if (sender is ListView listView)
		{
			listView.ScrollIntoView(listView.SelectedItem);
		}
	}
}
