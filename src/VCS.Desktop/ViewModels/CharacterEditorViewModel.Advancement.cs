using Microsoft.Extensions.Logging;
using VCS.Domain.Characters;

namespace VCS.Desktop.ViewModels;

public partial class CharacterEditorViewModel
{
    private AdvancementState? _advancement;
    private IReadOnlyList<AdvantageDefinition> _advantageDefinitions = [];
    private ExperienceCalculation? _experienceCalculation;
    public bool IsCreating => _advancement?.IsCreating == true;
    public bool CanEditBuild => true;
    public string[] AgeOptions { get; } = ["Crianca da noite", "Neofito", "Ancillae"];
    public string[] SkillProfileOptions { get; } = ["Especialista", "Equilibrado", "Pau para toda obra"];
    public int AgeIndex
    {
        get => (int)(_advancement?.Age ?? VampireAge.Child);
        set
        {
            if (!Enum.IsDefined((VampireAge)value) || AgeIndex == value) return;
            var previous = _advancement!.Age;
            _advancement = _advancement with { Age = (VampireAge)value };
            BloodPotency = Math.Clamp(BloodPotency + CharacterAdvancement.StartingBlood((VampireAge)value) -
                CharacterAdvancement.StartingBlood(previous), 0, 10);
            RefreshCreation();
            MarkChanged();
        }
    }
    public int SkillProfileIndex
    {
        get => (int)(_advancement?.Profile ?? SkillProfile.Specialist);
        set
        {
            if (!Enum.IsDefined((SkillProfile)value) || SkillProfileIndex == value) return;
            _advancement = _advancement! with { Profile = (SkillProfile)value };
            RefreshCreation();
            MarkChanged();
        }
    }
    public string InitialAttributesDescription => IsNpc
        ? "Atributos iniciais: NPC usa os tres grupos livres (Fisicos, Sociais e Mentais), de 0 a 5."
        : "Atributos iniciais: 1 atributo com 4 pontos, 3 com 3, 4 com 2 e 1 com 1.";
    public string InitialSkillsDescription =>
        $"Habilidades iniciais: {string.Join(", ", CharacterAdvancement.SkillDistribution((SkillProfile)SkillProfileIndex))}; demais em zero.";
    public string InitialDisciplinesDescription => "Pontos iniciais gratuitos: duas disciplinas do cla com 2 e 1 pontos, mais o bonus aplicado do predador. Excedentes: novo nivel x 5 XP (do cla) ou x 7 XP (fora do cla).";
    public string InitialAdvantagesDescription =>
        $"Pontos iniciais gratuitos: {CharacterAdvancement.AdvantageBudget((VampireAge)AgeIndex)} em vantagens (inclui loresheets). " +
        $"Desvantagens de referencia: {CharacterAdvancement.FlawBudget((VampireAge)AgeIndex)}. Vantagens excedentes: 3 XP por ponto; bonus de predador nao sao cobrados.";
    public string FreeSpecializationSummary =>
        $"Especializacoes gratuitas disponiveis: {CharacterAdvancement.FreeSpecializationsRemaining(Snapshot().Details, Snapshot().Traits)}. " +
        "1 por ficha; a especializacao escolhida no predador e adicional e aplicada automaticamente. +1 local ao atingir 4; " +
        "+1 local no primeiro ponto de Erudicao, Oficios, Performance e Ciencia. " +
        "Separe especializacoes por ponto e virgula. Excedentes: 3 XP cada, calculados automaticamente.";
    public int InitialExperience => CharacterAdvancement.StartingXp((VampireAge)AgeIndex);
    public int MaximumCampaignExperience => 1000000 - InitialExperience;
    public string ExperienceWarning => SectionExperience("Background");
    public string AttributesExperience => SectionExperience("Attributes");
    public string SkillsExperience => SectionExperience("Skills");
    public string DisciplinesExperience => SectionExperience("Disciplines");
    public string AdvantagesExperience => SectionExperience("Advantages");
    public string StateExperience => SectionExperience("State");
    public string[] ExperienceBreakdown => _experienceCalculation?.Costs.Select(x => $"{x.Name}: {x.Cost} XP.").ToArray() ?? [];
    private string SectionExperience(string section) => _experienceCalculation is not { } calculation ? "" :
        string.Join(Environment.NewLine, calculation.Costs.Where(x => x.Section == section).Select(x => $"{x.Name}: {x.Cost} XP.")
            .Concat(calculation.Warnings.Where(x => x.Field == section).Select(x => x.Message)));
    private void RecalculateExperience()
    {
        var ready = _ready;
        _ready = false;
        try
        {
            var snapshot = Snapshot();
            _experienceCalculation = AutomaticExperience.Calculate(snapshot.Details, snapshot.Traits);
            ExperienceTotal = _experienceCalculation.Total;
            ExperienceSpent = _experienceCalculation.Spent;
            CampaignExperienceSpent = _experienceCalculation.CampaignSpent;
            foreach (var property in new[] { nameof(InitialExperience), nameof(MaximumCampaignExperience), nameof(ExperienceWarning), nameof(AttributesExperience),
                nameof(SkillsExperience), nameof(DisciplinesExperience), nameof(AdvantagesExperience), nameof(StateExperience),
                nameof(InitialAdvantagesDescription), nameof(ExperienceBreakdown) })
                OnPropertyChanged(property);
            RefreshLoresheetSummary();
        }
        finally { _ready = ready; }
    }
    private void RefreshCreation()
    {
        foreach (var property in new[] { nameof(IsCreating), nameof(CanEditBuild), nameof(AgeIndex),
            nameof(SkillProfileIndex), nameof(InitialSkillsDescription), nameof(FreeSpecializationSummary) })
            OnPropertyChanged(property);
        RefreshLoresheetSummary();
    }
    private void ReportAdvancementError(InvalidOperationException exception)
    {
        _logger.LogWarning(exception, "Operacao da ficha recusada para {CharacterId}", Id);
        Error = exception.Message;
    }
}
