using KiloKilo.ViewModels;

namespace KiloKilo.Views;

public partial class HomePage : ResponsivePage
{
    public HomePage(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        HostView = Host;
    }
}
