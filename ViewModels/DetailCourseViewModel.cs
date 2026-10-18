using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KiloKilo.Helpers;
using KiloKilo.Models;
using KiloKilo.Services;

namespace KiloKilo.ViewModels;

[QueryProperty(nameof(CourseId), "id")]
[QueryProperty(nameof(Confirmation), "confirmation")]
public partial class DetailCourseViewModel : BaseViewModel
{
    private readonly IKiloKiloService _data;

    public DetailCourseViewModel(IKiloKiloService data) => _data = data;

    [ObservableProperty] private string? courseId;
    [ObservableProperty] private string? confirmation;
    [ObservableProperty] private Course? course;

    public bool IsConfirmation => Confirmation == "1";
    public bool IsNotConfirmation => !IsConfirmation;
    partial void OnConfirmationChanged(string? value)
    {
        OnPropertyChanged(nameof(IsConfirmation));
        OnPropertyChanged(nameof(IsNotConfirmation));
    }

    partial void OnCourseIdChanged(string? value)
    {
        if (Guid.TryParse(value, out var id)) _ = ChargerAsync(id);
    }

    private async Task ChargerAsync(Guid id) => Course = await _data.GetCourseAsync(id);

    /// <summary>Recharge la course au retour de l'écran de modification.</summary>
    public override async Task OnAppearingAsync()
    {
        if (Guid.TryParse(CourseId, out var id)) await ChargerAsync(id);
    }

    [RelayCommand]
    private Task RetourAsync() => Shell.Current.GoToAsync("..");

    [RelayCommand]
    private Task RetourAccueilAsync() => Shell.Current.GoToAsync(Routes.Accueil);

    [RelayCommand]
    private Task DupliquerAsync()
        => Course is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync($"../{Routes.NouvelleCourse}?from={Course.Id}");

    [RelayCommand]
    private Task ModifierAsync()
        => Course is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync($"{Routes.NouvelleCourse}?edit={Course.Id}");

    [RelayCommand]
    private async Task SupprimerAsync()
    {
        if (Course is null) return;
        if (!await ConfirmAsync("Supprimer cette course ?", "Cette action est définitive.", "Supprimer", "Annuler")) return;
        await _data.DeleteCourseAsync(Course.Id);
        await Shell.Current.GoToAsync(IsConfirmation ? Routes.Accueil : "..");
    }
}
