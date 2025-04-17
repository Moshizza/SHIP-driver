using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Prysm.AppVision.AppWizardSalto.ViewModels;
using Prysm.AppVision.Common;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace Prysm.AppVision.AppWizardSalto.Views;

public partial class Details : Page, IComponentConnector
{
	private Entity[] _selectedItems;

	internal RadGridView List;

	public Details()
	{
		InitializeComponent();
		_selectedItems = ((ViewModel)base.DataContext).SelectedEntities.ToArray();
	}

	private void List_Loaded(object sender, RoutedEventArgs e)
	{
		((GridViewDataControl)List).Select((IEnumerable)_selectedItems);
	}

	private void List_SelectionChanged(object sender, SelectionChangeEventArgs e)
	{
		((ObservableObject)(ViewModel)base.DataContext).Notify("SelectedEntities");
	}
}
