using KiloKilo.ViewModels;

namespace KiloKilo.Views;

public partial class CodeOubliePage : ResponsivePage
{
    public CodeOubliePage(CodeOublieViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        HostView = Host;
    }
}
