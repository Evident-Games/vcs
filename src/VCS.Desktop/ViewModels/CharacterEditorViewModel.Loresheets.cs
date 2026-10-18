using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Application.Disciplines;
using VCS.Domain.Characters;

namespace VCS.Desktop.ViewModels;

public partial class CharacterEditorViewModel
{
    private AdvantageDefinition[] _loresheetDefinitions = [];
    public ObservableCollection<string> LoresheetCollections { get; } = ["Todas as colecoes"];
    public ObservableCollection<AdvantageDefinition> LoresheetBenefits { get; } = [];
    [ObservableProperty] private string selectedLoresheetCollection = "Todas as colecoes";
    [ObservableProperty] private string loresheetSearch = "";
    [ObservableProperty] private AdvantageDefinition? selectedLoresheetBenefit;
    [ObservableProperty] private bool payLoresheetWithInitialXp;
    [ObservableProperty] private int campaignExperienceTotal;
    [ObservableProperty] private int campaignExperienceSpent;
    [ObservableProperty] private int campaignXpAward;
    public int CampaignExperienceAvailable => CampaignExperienceTotal - CampaignExperienceSpent;
    public int NonCampaignExperienceAvailable => ExperienceAvailable - CampaignExperienceAvailable;
    public string LoresheetDescription => SelectedLoresheetBenefit?.Description ?? "Selecione um beneficio da colecao.";
    public string LoresheetCost => SelectedLoresheetBenefit is { } benefit
        ? $"{benefit.LoresheetCollection} / {(benefit.IsLineage ? "Linhagem/descendencia" : "Geral")} / {benefit.Minimum} ponto(s), comprado separadamente. " +
          (IsCreating ? $"Pague {benefit.Minimum} pontos de vantagens OU {benefit.Minimum * 3} XP inicial de idade."
              : $"Apos a criacao: {benefit.Minimum * 3} XP exclusivamente de campanha.")
        : "";
    public string LoresheetRulesSummary => "Loresheets sao vantagens por colecao, mas cada beneficio e independente: nao exige comprar os anteriores. " +
        "Criacao: no maximo um beneficio geral + um de linhagem/descendencia. As quatro colecoes fornecidas sao gerais. " +
        "Depois: sem limite de colecoes, pagando pontos x 3 somente com XP de campanha. Efeitos sao referencias; nao alteram dados ou trilhas automaticamente.";
    public string CampaignXpSummary => $"Saldo de campanha: {CampaignExperienceAvailable} XP. Saldo inicial/anterior: {NonCampaignExperienceAvailable} XP. " +
        "Em fichas antigas, o XP existente nao e presumido como campanha. Classifique somente o saldo de campanha conhecido em Criacao e evolucao / XP; ele faz parte do total, nao e somado novamente.";
    partial void OnCampaignExperienceTotalChanged(int value) => RefreshLoresheetSummary();
    partial void OnCampaignExperienceSpentChanged(int value) => RefreshLoresheetSummary();
    partial void OnSelectedLoresheetCollectionChanged(string value) => FilterLoresheets();
    partial void OnLoresheetSearchChanged(string value) => FilterLoresheets();
    partial void OnSelectedLoresheetBenefitChanged(AdvantageDefinition? value) => RefreshLoresheetSummary();

    private void ConfigureLoresheets(IReadOnlyList<AdvantageDefinition> entries)
    {
        _loresheetDefinitions = entries.Where(x => x.Category == TraitCategory.Loresheet).ToArray();
        var current = SelectedLoresheetCollection;
        LoresheetCollections.Clear();
        LoresheetCollections.Add("Todas as colecoes");
        foreach (var collection in _loresheetDefinitions.Select(x => x.LoresheetCollection).Distinct().Order())
            LoresheetCollections.Add(collection);
        SelectedLoresheetCollection = LoresheetCollections.Contains(current) ? current : "Todas as colecoes";
        FilterLoresheets();
    }
    private void FilterLoresheets()
    {
        var selectedId = SelectedLoresheetBenefit?.Id;
        var search = CatalogText.Normalize(LoresheetSearch);
        LoresheetBenefits.Clear();
        foreach (var item in _loresheetDefinitions.Where(x =>
            (SelectedLoresheetCollection == "Todas as colecoes" || x.LoresheetCollection == SelectedLoresheetCollection) &&
            CatalogText.Normalize($"{x.Name} {x.LoresheetCollection} {x.Description}").Contains(search, StringComparison.Ordinal))
            .OrderBy(x => x.LoresheetCollection).ThenBy(x => x.Minimum))
            LoresheetBenefits.Add(item);
        SelectedLoresheetBenefit = LoresheetBenefits.FirstOrDefault(x => x.Id == selectedId) ?? LoresheetBenefits.FirstOrDefault();
    }
    private void RefreshLoresheetSummary()
    {
        foreach (var name in new[] { nameof(LoresheetDescription), nameof(LoresheetCost), nameof(CampaignXpSummary),
            nameof(CampaignExperienceAvailable), nameof(NonCampaignExperienceAvailable) })
            OnPropertyChanged(name);
    }
    [RelayCommand]
    private void AddLoresheet()
    {
        try
        {
            if (SelectedLoresheetBenefit is not { } item) throw new InvalidOperationException("Selecione um beneficio de loresheet.");
            var snapshot = Snapshot();
            var fundingIssues = CharacterRules.Validate(snapshot.Details, snapshot.Traits);
            if (fundingIssues.Count != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, fundingIssues.Select(x => x.Message)));
            var payment = IsCreating ? PayLoresheetWithInitialXp ? LoresheetPayment.InitialXp : LoresheetPayment.AdvantagePoints : LoresheetPayment.CampaignXp;
            var result = LoresheetRules.Select(snapshot.Details, snapshot.Traits,
                new(TraitCategory.Loresheet, item.Id, item.Name, item.Minimum,
                    Loresheet: new(item.LoresheetCollection, item.IsLineage, item.Description, payment)), payment);
            _ready = false;
            try
            {
                _advancement = result.Details.Advancement;
                ExperienceSpent = result.Details.ExperienceSpent;
                CampaignExperienceSpent = result.Details.CampaignExperienceSpent;
                Loresheets.AddEntry(result.Traits.Single(x => x.Category == TraitCategory.Loresheet && x.Key == item.Id));
            }
            finally { _ready = true; }
            MarkChanged();
        }
        catch (InvalidOperationException exception) { ReportAdvancementError(exception); }
    }
    [RelayCommand]
    private void RemoveInitialLoresheet(TraitItemViewModel? item)
    {
        try
        {
            if (item is null) throw new InvalidOperationException("Selecione um beneficio da ficha.");
            var snapshot = Snapshot();
            var result = LoresheetRules.RemoveInitial(snapshot.Details, snapshot.Traits, item.Key);
            _ready = false;
            try { Loresheets.RemoveCommand.Execute(item); ExperienceSpent = result.Details.ExperienceSpent; }
            finally { _ready = true; }
            MarkChanged();
        }
        catch (InvalidOperationException exception) { ReportAdvancementError(exception); }
    }
    [RelayCommand]
    private void AwardCampaignXp()
    {
        if (CampaignXpAward <= 0 || (long)ExperienceTotal + CampaignXpAward > 1000000)
        {
            ReportAdvancementError(new("Informe um premio de campanha positivo, sem ultrapassar o limite de 1.000.000 XP."));
            return;
        }
        ExperienceTotal += CampaignXpAward;
        CampaignExperienceTotal += CampaignXpAward;
        CampaignXpAward = 0;
    }
}
