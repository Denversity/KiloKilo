using KiloKilo.Helpers;
using Microsoft.Maui.Controls.Shapes;

namespace KiloKilo.Controls;

/// <summary>
/// Barre de navigation inférieure personnalisée (remplace la barre native du Shell, dont l'indicateur
/// de sélection n'est pas homogène selon les plateformes). L'indicateur couvre toute la largeur de l'onglet actif.
/// Usage : &lt;controls:BottomNavBar Active="home" /&gt;
/// </summary>
public class BottomNavBar : ContentView
{
    private sealed record NavEntry(string Key, string Titre, string Route, string Icone);

    private static readonly NavEntry[] Entries =
    {
        new("home", "Accueil", Routes.Accueil,
            "M12,3 L3,10.5 L3,21 L9,21 L9,15 L15,15 L15,21 L21,21 L21,10.5 Z"),
        new("historique", "Historique", Routes.Historique,
            "M4,5 L20,5 L20,8 L4,8 Z M4,10.5 L20,10.5 L20,13.5 L4,13.5 Z M4,16 L20,16 L20,19 L4,19 Z"),
        new("prix", "Prix", Routes.Prix,
            "M3,3 L12,3 L21,12 L12,21 L3,12 Z"),
        new("stats", "Stats", Routes.Stats,
            "M4,20 L4,11 L8,11 L8,20 Z M10,20 L10,4 L14,4 L14,20 Z M16,20 L16,14 L20,14 L20,20 Z"),
        new("profil", "Profil", Routes.Profil,
            "M12,3 L15.5,4.5 L17,8 L15.5,11.5 L12,13 L8.5,11.5 L7,8 L8.5,4.5 Z M4,21 L4,18.5 L8,15 L16,15 L20,18.5 L20,21 Z"),
    };

    private static readonly Color Primary = Color.FromArgb("#4A7C59");
    private static readonly Color PrimaryDark = Color.FromArgb("#7FBF93");
    private static readonly Color Muted = Color.FromArgb("#6B7468");
    private static readonly Color MutedDark = Color.FromArgb("#9AA59C");

    public static readonly BindableProperty ActiveProperty = BindableProperty.Create(
        nameof(Active), typeof(string), typeof(BottomNavBar), string.Empty,
        propertyChanged: (b, _, _) => ((BottomNavBar)b).Build());

    public string Active
    {
        get => (string)GetValue(ActiveProperty);
        set => SetValue(ActiveProperty, value);
    }

    public BottomNavBar() => Build();

    private void Build()
    {
        var items = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
        for (var i = 0; i < Entries.Length; i++)
        {
            items.AddColumnDefinition(new ColumnDefinition(GridLength.Star));
            var item = BuildItem(Entries[i], Entries[i].Key == Active);
            Grid.SetColumn(item, i);
            items.Children.Add(item);
        }

        var line = new BoxView { HeightRequest = 1 };
        line.SetAppThemeColor(BoxView.ColorProperty, Color.FromArgb("#E6E0D2"), Color.FromArgb("#2A332D"));

        var root = new Grid { RowSpacing = 0, Padding = new Thickness(0, 0, 0, 4) };
        root.AddRowDefinition(new RowDefinition(1));
        root.AddRowDefinition(new RowDefinition(GridLength.Auto));
        root.Children.Add(line);
        Grid.SetRow(items, 1);
        root.Children.Add(items);
        root.SetAppThemeColor(BackgroundColorProperty, Colors.White, Color.FromArgb("#18201B"));

        Content = root;
    }

    private static View BuildItem(NavEntry e, bool active)
    {
        // Indicateur sur toute la largeur de l'onglet actif
        var indicator = new BoxView { HeightRequest = 3, IsVisible = active, VerticalOptions = LayoutOptions.Start };
        indicator.SetAppThemeColor(BoxView.ColorProperty, Primary, PrimaryDark);

        var icon = new Microsoft.Maui.Controls.Shapes.Path
        {
            Data = (Geometry)new PathGeometryConverter().ConvertFromInvariantString(e.Icone)!,
            Aspect = Stretch.Uniform,
            WidthRequest = 22,
            HeightRequest = 22,
            HorizontalOptions = LayoutOptions.Center
        };
        icon.SetAppTheme<Brush>(Shape.FillProperty,
            new SolidColorBrush(active ? Primary : Muted),
            new SolidColorBrush(active ? PrimaryDark : MutedDark));

        var label = new Label
        {
            Text = e.Titre,
            FontSize = 11,
            FontFamily = active ? "NunitoSansBold" : "NunitoSansSemiBold",
            HorizontalOptions = LayoutOptions.Center
        };
        label.SetAppThemeColor(Label.TextColorProperty, active ? Primary : Muted, active ? PrimaryDark : MutedDark);

        var content = new VerticalStackLayout
        {
            Spacing = 3,
            Padding = new Thickness(0, 10, 0, 6),
            Children = { icon, label }
        };

        var item = new Grid { BackgroundColor = Colors.Transparent, Children = { content, indicator } };
        SemanticProperties.SetDescription(item, e.Titre);
        item.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                if (!active) await Shell.Current.GoToAsync(e.Route);
            })
        });
        return item;
    }
}
