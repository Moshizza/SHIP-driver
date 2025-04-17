using System.Windows.Controls;
using System.Windows.Markup;
using Telerik.Windows.Controls;

namespace Prysm.AppVision.AppWizardSalto.Views;

public partial class AppLogin : Page, IComponentConnector
{
	internal RadPasswordBox PasswordField;

	public AppLogin()
	{
		InitializeComponent();
	}
}
