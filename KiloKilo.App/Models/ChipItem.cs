using CommunityToolkit.Mvvm.ComponentModel;

namespace KiloKilo.Models;

/// <summary>Élément sélectionnable d'une barre de filtres (chips).</summary>
public partial class ChipItem : ObservableObject
{
    public ChipItem(string label, bool isSelected = false)
    {
        Label = label;
        this.isSelected = isSelected;
    }

    public string Label { get; }

    [ObservableProperty]
    private bool isSelected;
}
