using System.ComponentModel;
using KiloKilo.Controls;
using KiloKilo.ViewModels;

namespace KiloKilo.Views;

public partial class ProduitDetailPage : ResponsivePage
{
    private readonly ProduitDetailViewModel _vm;

    public ProduitDetailPage(ProduitDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        HostView = Host;
        _vm.PropertyChanged += OnVmChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (Application.Current is not null) Application.Current.RequestedThemeChanged += OnThemeChanged;
        Redessiner();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (Application.Current is not null) Application.Current.RequestedThemeChanged -= OnThemeChanged;
    }

    private void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProduitDetailViewModel.Produit)) Redessiner();
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => Redessiner();

    private void Redessiner()
    {
        var points = _vm.Produit?.Points ?? new();
        Chart.Drawable = new PrixChartDrawable(points, Application.Current?.RequestedTheme == AppTheme.Dark);
        Chart.Invalidate();
    }
}
