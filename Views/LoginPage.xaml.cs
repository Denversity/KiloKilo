using KiloKilo.ViewModels;

namespace KiloKilo.Views;

public partial class LoginPage : ResponsivePage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        HostView = Host;
    }
}
