using System.Windows.Controls;
using System.Windows.Markup;
using Prysm.AppVision.AppWizardSalto.ViewModels;
using Telerik.Windows.Controls;

namespace Prysm.AppVision.AppWizardSalto.Views;

public partial class AdvancedOptions : Page, IComponentConnector
{
	internal RadPasswordBox pwdBox;

	public AdvancedOptions()
	{
		InitializeComponent();
	}

	private void RadPasswordBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		((ViewModel)base.DataContext).HttpsPassword = pwdBox.Password;
	}
}
