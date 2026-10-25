using KiloKilo.ViewModels;

namespace KiloKilo.Views;

public partial class ProduitsPage : ResponsivePage
{
    public ProduitsPage(ProduitsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        HostView = Host;
    }
}
