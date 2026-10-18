using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VCS.Domain.Characters;
using VCS.Application.Disciplines;

namespace VCS.Desktop.ViewModels;

public sealed record AdvancementOption(TraitCategory? Category, string Key, string Name)
{
    public string Label => Category is null ? Name : $"{Category switch
    {
        TraitCategory.Attribute => "Atributo", TraitCategory.Skill => "Habilidade",
        TraitCategory.Discipline => "Disciplina", _ => "Vantagem"
    }} / {Name}";
}

public partial class CharacterEditorViewModel
{
    private AdvancementState? _advancement;
    private IReadOnlyList<AdvantageDefinition> _advantageDefinitions = [];
    public bool HasAdvancement => _advancement is not null;
    public bool IsCreating => _advancement?.IsCreating == true;
    public bool IsEvolving => _advancement?.IsCreating == false;
    public bool CanEditBuild => !IsEvolving;
    public bool CanStartAdvancement => !HasAdvancement;
    public string[] AgeOptions { get; } = ["Crianca da noite", "Neofito", "Ancillae"];
    public string[] SkillProfileOptions { get; } = ["Especialista", "Equilibrado", "Pau para toda obra"];
    public int AgeIndex
    {
        get => (int)(_advancement?.Age ?? VampireAge.Child);
        set
        {
            if (!IsCreating || !Enum.IsDefined((VampireAge)value) || AgeIndex == value) return;
            var previous = _advancement!.Age;
            _advancement = _advancement with { Age = (VampireAge)value };
            ExperienceTotal += CharacterAdvancement.StartingXp((VampireAge)value) - CharacterAdvancement.StartingXp(previous);
            BloodPotency = CharacterAdvancement.StartingBlood((VampireAge)value);
            RefreshCreation();
            MarkChanged();
        }
    }
    public int SkillProfileIndex
    {
        get => (int)(_advancement?.Profile ?? SkillProfile.Specialist);
        set
        {
            if (!IsCreating || !Enum.IsDefined((SkillProfile)value) || SkillProfileIndex == value) return;
            _advancement = _advancement! with { Profile = (SkillProfile)value };
            RefreshCreation();
            MarkChanged();
        }
    }
    public string CreationSummary
    {
        get
        {
            if (_advancement is null) return "Ficha livre. Nenhum XP retroativo sera calculado. Registre a base para acompanhar futuras compras.";
            if (!IsCreating) return "Base inicial registrada. Evolua por compras abaixo; os pontos iniciais nao contam como XP gasto.";
            return $"Atributos: 1 x 4, 3 x 3, 4 x 2, 1 x 1 (NPC usa os tres grupos livres).\n" +
                $"Habilidades: {string.Join(", ", CharacterAdvancement.SkillDistribution(_advancement.Profile))}; demais em zero.\n" +
                $"Disciplinas: duas do cla, com 2 e 1 pontos. Vantagens (inclui loresheets pagos em pontos): {Advantages.Items.Sum(x => x.Rating) + LoresheetRules.AdvantagePoints(Loresheets.Items.Select(x => x.ToValue()))}/{CharacterAdvancement.AdvantageBudget(_advancement.Age)}. " +
                $"Desvantagens: {Flaws.Items.Sum(x => x.Rating)}/{CharacterAdvancement.FlawBudget(_advancement.Age)}.\n" +
                $"Bonus inicial: {CharacterAdvancement.StartingXp(_advancement.Age)} XP; Potencia de Sangue {CharacterAdvancement.StartingBlood(_advancement.Age)}.\n" +
                "Predadores: bonus de disciplina pendente de cadastro. Uma especializacao de predador fica disponivel ao preencher o tipo de predador.";
        }
    }
    public string CreationValidation => !IsCreating ? "" :
        string.Join(Environment.NewLine, CharacterAdvancement.CreationIssues(Snapshot().Details, Snapshot().Traits));
    public string FreeSpecializationSummary => !HasAdvancement ? "" :
        $"Especializacoes gratuitas disponiveis: {CharacterAdvancement.FreeSpecializationsRemaining(Snapshot().Details, Snapshot().Traits)}. " +
        "1 por ficha + 1 de predador (se informado); +1 local ao atingir 4; +1 local no primeiro ponto de Erudicao, Oficios, Performance e Ciencia. " +
        "Separe especializacoes por ponto e virgula. Demais: 3 XP cada.";
    public ObservableCollection<AdvancementOption> AdvancementOptions { get; } = [];
    [ObservableProperty] private AdvancementOption? selectedAdvancement;
    [ObservableProperty] private string newSpecialization = "";
    public string[] PurchaseHistory => _advancement?.Purchases.Reverse().Select(x =>
        $"{x.Utc.ToLocalTime():dd/MM/yyyy HH:mm} | {x.Name}: " +
        (x.Specialization.Length == 0 ? $"{x.From} -> {x.To}" : x.Specialization) + $" | {x.Cost} XP ({x.CampaignCost} campanha)" +
        (x.Loresheet is null ? "" : $" | {x.Loresheet.Collection}")).ToArray() ?? [];
    public string PurchasePreview
    {
        get
        {
            if (SelectedAdvancement is not { } selected) return "Selecione o traco a evoluir.";
            var snapshot = Snapshot();
            var trait = snapshot.Traits.FirstOrDefault(x => x.Category == selected.Category && x.Key == selected.Key);
            if (IsNpc && selected.Category is null && selected.Key != "BloodPotency")
                trait = new(TraitCategory.Attribute, selected.Key, selected.Name,
                    selected.Key == "NpcPhysical" ? NpcPhysical : selected.Key == "NpcSocial" ? NpcSocial : NpcMental);
            if (trait is null && selected.Category is not null)
                trait = new(selected.Category.Value, selected.Key, selected.Name, 0);
            try { return $"Proximo ponto: {CharacterAdvancement.NextCost(snapshot.Details, trait)} XP. Especializacoes usam primeiro os bonus gratuitos aplicaveis."; }
            catch (InvalidOperationException exception) { return exception.Message; }
        }
    }
    partial void OnSelectedAdvancementChanged(AdvancementOption? value) => OnPropertyChanged(nameof(PurchasePreview));

    private void RefreshCreation()
    {
        foreach (var property in new[] { nameof(HasAdvancement), nameof(IsCreating), nameof(IsEvolving), nameof(CanEditBuild),
            nameof(CanStartAdvancement), nameof(AgeIndex), nameof(SkillProfileIndex), nameof(CreationSummary),
            nameof(CreationValidation), nameof(FreeSpecializationSummary), nameof(PurchaseHistory), nameof(PurchasePreview) })
            OnPropertyChanged(property);
        RefreshLoresheetSummary();
        var choices = Attributes.Items.Where(_ => !IsNpc).Concat(Skills.Items).Concat(Disciplines.Items).Concat(Advantages.Items)
            .Select(x => new AdvancementOption(x.Category, x.Key, x.Name))
            .Concat(_advantageDefinitions.Where(x => x.Category == TraitCategory.Advantage)
                .Select(x => new AdvancementOption(x.Category, x.Id, x.Name)))
            .Concat(IsNpc ? Powers.AllPowers.Where(x => !x.Definition.IsRitual).Select(x => x.Definition.Discipline)
                .Distinct().Where(x => !Disciplines.Items.Any(item => ClanCatalog.SameName(item.Name, x)))
                .Select(x => new AdvancementOption(TraitCategory.Discipline, CatalogText.StableId("disc-", x), x)) : [])
            .DistinctBy(x => (x.Category, x.Key)).Append(new(null, "BloodPotency", "Potencia de Sangue"))
            .Concat(IsNpc ? new AdvancementOption[] { new(null, "NpcPhysical", "Fisicos"),
                new(null, "NpcSocial", "Sociais"), new(null, "NpcMental", "Mentais") } : []).ToArray();
        if (!AdvancementOptions.SequenceEqual(choices))
        {
            var selected = SelectedAdvancement;
            AdvancementOptions.Clear();
            foreach (var option in choices) AdvancementOptions.Add(option);
            SelectedAdvancement = choices.FirstOrDefault(x => x == selected) ?? choices.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void StartCreation()
    {
        if (HasAdvancement) return;
        _advancement = new();
        ExperienceTotal = 0;
        ExperienceSpent = 0;
        CampaignExperienceTotal = 0;
        CampaignExperienceSpent = 0;
        BloodPotency = 0;
        RefreshCreation();
        MarkChanged();
    }

    [RelayCommand]
    private void TrackExisting()
    {
        if (HasAdvancement) return;
        var snapshot = Snapshot();
        _advancement = CharacterAdvancement.EstablishBaseline(snapshot.Details, snapshot.Traits).Advancement;
        RefreshCreation();
        MarkChanged();
    }

    [RelayCommand]
    private void FinishCreation()
    {
        try
        {
            var snapshot = Snapshot();
            _advancement = CharacterAdvancement.FinalizeCreation(snapshot.Details, snapshot.Traits).Advancement;
            RefreshCreation();
            MarkChanged();
        }
        catch (InvalidOperationException exception) { ReportAdvancementError(exception); }
    }

    [RelayCommand] private void BuyLevel() => Purchase(false);
    [RelayCommand] private void BuySpecialization() => Purchase(true);
    private void Purchase(bool specialization)
    {
        try
        {
            if (SelectedAdvancement is not { } selected) throw new InvalidOperationException("Selecione um traco.");
            if (specialization && string.IsNullOrWhiteSpace(NewSpecialization))
                throw new InvalidOperationException("Informe o texto da especializacao.");
            var snapshot = Snapshot();
            var result = CharacterAdvancement.Buy(snapshot.Details, snapshot.Traits, selected.Category, selected.Key,
                specialization ? NewSpecialization : "", selected.Name);
            _ready = false;
            try
            {
                _advancement = result.Details.Advancement;
                ExperienceSpent = result.Details.ExperienceSpent;
                CampaignExperienceSpent = result.Details.CampaignExperienceSpent;
                BloodPotency = result.Details.BloodPotency;
                NpcPhysical = result.Details.NpcPhysical;
                NpcSocial = result.Details.NpcSocial;
                NpcMental = result.Details.NpcMental;
                foreach (var value in result.Traits.Where(x => x.Category != TraitCategory.DisciplinePower))
                {
                    var group = value.Category switch
                    {
                        TraitCategory.Attribute => Attributes, TraitCategory.Skill => Skills,
                        TraitCategory.Discipline => Disciplines, TraitCategory.Advantage => Advantages,
                        TraitCategory.Flaw => Flaws, TraitCategory.Loresheet => Loresheets,
                        _ => throw new InvalidOperationException("Categoria de traco invalida.")
                    };
                    var item = group.Items.FirstOrDefault(x => x.Key == value.Key);
                    if (item is null) group.AddEntry(value);
                    else { item.Rating = value.Rating; item.Specialization = value.Specialization; }
                }
                if (specialization) NewSpecialization = "";
                if (IsNpc)
                {
                    NpcDisciplines.Clear();
                    foreach (var item in Disciplines.Items) NpcDisciplines.Add(new(this, item));
                    while (NpcDisciplines.Count < 6) NpcDisciplines.Add(new(this));
                    ConfigureAdvantageCatalog(_advantageDefinitions);
                }
            }
            finally { _ready = true; }
            RefreshAdvancement();
            RefreshCreation();
            OnPropertyChanged(nameof(HealthCapacity));
            OnPropertyChanged(nameof(WillpowerCapacity));
            MarkChanged();
        }
        catch (InvalidOperationException exception) { ReportAdvancementError(exception); }
    }

    private void ReportAdvancementError(InvalidOperationException exception)
    {
        _logger.LogWarning(exception, "Operacao de evolucao recusada para {CharacterId}", Id);
        Error = exception.Message;
    }
}
