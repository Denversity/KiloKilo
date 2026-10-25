using KiloKilo.ViewModels;

namespace KiloKilo.Views;

public partial class NouvelleCoursePage : ResponsivePage
{
    public NouvelleCoursePage(NouvelleCourseViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        HostView = Host;
    }
}
