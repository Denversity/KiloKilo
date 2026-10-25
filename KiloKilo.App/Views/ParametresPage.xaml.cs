using KiloKilo.ViewModels;

namespace KiloKilo.Views;

public partial class ParametresPage : ResponsivePage
{
    public ParametresPage(ParametresViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        HostView = Host;
    }
}
