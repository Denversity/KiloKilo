using KiloKilo.ViewModels;

namespace KiloKilo.Views;

public partial class HistoriquePage : ResponsivePage
{
    public HistoriquePage(HistoriqueViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        HostView = Host;
    }
}
