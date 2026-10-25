using KiloKilo.ViewModels;

namespace KiloKilo.Views;

public partial class RegisterPage : ResponsivePage
{
    public RegisterPage(RegisterViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        HostView = Host;
    }
}
