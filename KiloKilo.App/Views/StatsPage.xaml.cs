using KiloKilo.ViewModels;

namespace KiloKilo.Views;

public partial class StatsPage : ResponsivePage
{
    public StatsPage(StatsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        HostView = Host;
    }
}
