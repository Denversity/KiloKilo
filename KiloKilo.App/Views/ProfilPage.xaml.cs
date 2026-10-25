using KiloKilo.ViewModels;

namespace KiloKilo.Views;

public partial class ProfilPage : ResponsivePage
{
    public ProfilPage(ProfilViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        HostView = Host;
    }
}
