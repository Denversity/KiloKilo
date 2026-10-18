using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KiloKilo.Helpers;
using KiloKilo.Models;
using KiloKilo.Services;

namespace KiloKilo.ViewModels;

[QueryProperty(nameof(DupliquerId), "from")]
[QueryProperty(nameof(EditId), "edit")]
public partial class NouvelleCourseViewModel : BaseViewModel
{
    private const string DraftKey = "brouillon_course";
    private readonly IKiloKiloService _data;
    private IReadOnlyList<ProduitStat> _produits = Array.Empty<ProduitStat>();
    private Course? _edition;
    private bool _loaded;
    private decimal _depenseMois;

    public NouvelleCourseViewModel(IKiloKiloService data)
    {
        _data = data;
        Marches = new ObservableCollection<ChipItem>
        {
            new("Mokolo"), new("Marché Central"), new("Mfoundi"), new("Autre")
        };
        Articles.CollectionChanged += (_, _) => Recalculer();
    }

    public ObservableCollection<ChipItem> Marches { get; }
    public ObservableCollection<ArticleCourse> Articles { get; } = new();

    public string DateAffichee => _edition is null ? $"Aujourd'hui, {DateTime.Today:dd MMM}." : _edition.DateLongue;

    [ObservableProperty] private string titre = "Nouvelle course";
    [ObservableProperty] private string libelleValider = "Valider et enregistrer le panier";
    [ObservableProperty] private bool isCreation = true;

    [ObservableProperty] private bool isAjoutVisible;
    [ObservableProperty] private string nouveauNom = string.Empty;
    [ObservableProperty] private string nouvelleQuantite = string.Empty;
    [ObservableProperty] private string nouveauPrix = string.Empty;
    [ObservableProperty] private string hintPrix = string.Empty;
    [ObservableProperty] private string erreur = string.Empty;
    [ObservableProperty] private string totalAffiche = Fcfa.Format(0);
    [ObservableProperty] private string nbArticlesAffiche = "0 article saisi";
    [ObservableProperty] private string budgetRestantAffiche = string.Empty;
    [ObservableProperty] private bool estVide = true;
    [ObservableProperty] private string? dupliquerId;
    [ObservableProperty] private string? editId;

    public bool HasErreur => !string.IsNullOrEmpty(Erreur);
    public bool HasHint => !string.IsNullOrEmpty(HintPrix);
    partial void OnErreurChanged(string value) => OnPropertyChanged(nameof(HasErreur));
    partial void OnHintPrixChanged(string value) => OnPropertyChanged(nameof(HasHint));

    /// <summary>Repère de prix : rappelle le dernier prix payé pour le produit en cours de saisie.</summary>
    partial void OnNouveauNomChanged(string value)
    {
        var t = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (t.Length < 3) { HintPrix = string.Empty; return; }
        var p = _produits.FirstOrDefault(x => x.Nom.ToLowerInvariant() == t)
                ?? _produits.FirstOrDefault(x => x.Nom.ToLowerInvariant().StartsWith(t));
        HintPrix = p?.DernierReleve ?? string.Empty;
    }

    partial void OnDupliquerIdChanged(string? value)
    {
        if (Guid.TryParse(value, out var id)) _ = ChargerDepuisAsync(id, false);
    }

    partial void OnEditIdChanged(string? value)
    {
        if (Guid.TryParse(value, out var id)) _ = ChargerDepuisAsync(id, true);
    }

    public override async Task OnAppearingAsync()
    {
        var courses = await _data.GetCoursesAsync();
        var auj = DateTime.Today;
        _depenseMois = courses.Where(c => c.Date.Year == auj.Year && c.Date.Month == auj.Month).Sum(c => c.Total);
        _produits = await _data.GetProduitsAsync();

        if (!_loaded)
        {
            _loaded = true;
            if (string.IsNullOrEmpty(DupliquerId) && string.IsNullOrEmpty(EditId)) RestaurerBrouillon();
            if (!Marches.Any(m => m.IsSelected)) Marches[1].IsSelected = true; // Marché Central par défaut
        }
        Recalculer();
    }

    private async Task ChargerDepuisAsync(Guid id, bool edition)
    {
        var src = await _data.GetCourseAsync(id);
        if (src is null) return;
        _loaded = true;

        Articles.Clear();
        foreach (var a in src.Articles) Articles.Add(a.Clone());
        foreach (var m in Marches) m.IsSelected = src.Marche.Contains(m.Label.Replace("Marché ", ""), StringComparison.OrdinalIgnoreCase);

        if (edition)
        {
            _edition = src;
            Titre = "Modifier la course";
            LibelleValider = "Enregistrer les modifications";
            IsCreation = false;
            OnPropertyChanged(nameof(DateAffichee));
        }
        Recalculer();
    }

    private void Recalculer()
    {
        var total = Articles.Sum(a => a.Prix);
        // En modification, la course d'origine est déjà comptée dans la dépense du mois
        var dejaCompte = _edition is not null && _edition.Date.Month == DateTime.Today.Month ? _edition.Total : 0;
        TotalAffiche = Fcfa.Format(total);
        NbArticlesAffiche = $"{Articles.Count} article{(Articles.Count > 1 ? "s" : "")} saisi{(Articles.Count > 1 ? "s" : "")}";
        EstVide = Articles.Count == 0;
        BudgetRestantAffiche = Fcfa.Format(_data.Profil.BudgetMensuel - (_depenseMois - dejaCompte) - total);
    }

    [RelayCommand]
    private void ChoisirMarche(ChipItem chip)
    {
        foreach (var m in Marches) m.IsSelected = m == chip;
    }

    [RelayCommand]
    private void BasculerAjout()
    {
        IsAjoutVisible = !IsAjoutVisible;
        Erreur = string.Empty;
    }

    [RelayCommand]
    private void AjouterArticle()
    {
        Erreur = string.Empty;
        if (string.IsNullOrWhiteSpace(NouveauNom)) { Erreur = "Indiquez le nom du produit."; return; }
        if (!Fcfa.TryParse(NouveauPrix, out var prix) || prix <= 0) { Erreur = "Indiquez un prix valide en FCFA."; return; }

        Articles.Add(new ArticleCourse { Nom = NouveauNom.Trim(), Quantite = NouvelleQuantite.Trim(), Prix = prix });
        NouveauNom = NouvelleQuantite = NouveauPrix = string.Empty;
        IsAjoutVisible = false;
    }

    [RelayCommand]
    private void SupprimerArticle(ArticleCourse article) => Articles.Remove(article);

    private string NomMarcheChoisi()
    {
        var m = Marches.First(x => x.IsSelected).Label;
        if (m == "Autre") return "Autre marché";
        return m.StartsWith("Marché") ? m : $"Marché {m}";
    }

    [RelayCommand]
    private async Task ValiderAsync()
    {
        Erreur = string.Empty;
        if (!Marches.Any(m => m.IsSelected)) { Erreur = "Choisissez le lieu des achats."; return; }
        if (Articles.Count == 0) { Erreur = "Ajoutez au moins un produit."; return; }

        if (_edition is not null)
        {
            _edition.Marche = NomMarcheChoisi();
            _edition.Articles = Articles.ToList();
            await _data.UpdateCourseAsync(_edition);
            await Shell.Current.GoToAsync("..");
            return;
        }

        var course = new Course { Marche = NomMarcheChoisi(), Date = DateTime.Today, Articles = Articles.ToList() };
        await _data.AddCourseAsync(course);
        Preferences.Default.Remove(DraftKey);

        // Nouvelle course → Détails du panier → Validation (écran récapitulatif avec bandeau de confirmation)
        await Shell.Current.GoToAsync($"../{Routes.DetailCourse}?id={course.Id}&confirmation=1");
    }

    [RelayCommand]
    private async Task BrouillonAsync()
    {
        var draft = new Draft(
            Marches.FirstOrDefault(m => m.IsSelected)?.Label,
            Articles.Select(a => new DraftItem(a.Nom, a.Quantite, a.Prix)).ToList());
        Preferences.Default.Set(DraftKey, JsonSerializer.Serialize(draft));
        await AlertAsync("Brouillon enregistré", "Vous retrouverez ce panier à votre prochaine saisie.");
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task AnnulerAsync()
    {
        if (Articles.Count > 0 && !await ConfirmAsync("Quitter la saisie ?", "Les modifications non enregistrées seront perdues.", "Quitter", "Continuer"))
            return;
        await Shell.Current.GoToAsync("..");
    }

    private void RestaurerBrouillon()
    {
        var json = Preferences.Default.Get(DraftKey, string.Empty);
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            var draft = JsonSerializer.Deserialize<Draft>(json);
            if (draft is null) return;
            foreach (var m in Marches) m.IsSelected = m.Label == draft.Marche;
            foreach (var i in draft.Items) Articles.Add(new ArticleCourse { Nom = i.Nom, Quantite = i.Quantite, Prix = i.Prix });
        }
        catch { Preferences.Default.Remove(DraftKey); }
    }

    private record DraftItem(string Nom, string Quantite, decimal Prix);
    private record Draft(string? Marche, List<DraftItem> Items);
}
