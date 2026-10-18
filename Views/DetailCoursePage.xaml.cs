using KiloKilo.ViewModels;

namespace KiloKilo.Views;

public partial class DetailCoursePage : ResponsivePage
{
    public DetailCoursePage(DetailCourseViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        HostView = Host;
    }
}
