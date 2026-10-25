using Microsoft.Extensions.Logging;
using VCS.Domain.Advancement;
using VCS.Domain.Advantages;
using VCS.Domain.Characters;
using VCS.Domain.Clans;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    private AdvancementState? _advancement;
    private IReadOnlyList<AdvantageDefinition> _advantageDefinitions = [];
    private ExperienceCalculation? _experienceCalculation;
    public bool IsCreating => _advancement?.IsCreating == true;
    public bool CanEditBuild => true;
    public string[] AgeOptions { get; } = ["Criança da Noite", "Neófito", "Ancillae"];
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
            MarkChanged();
        }
    }
    public bool IsAttributeBaseLocked => _advancement?.AttributeBase.Length > 0;
    private bool IsAttributeSpreadCustom => !IsAttributeBaseLocked &&
        CharacterAdvancement.MissingDistribution(Attributes.Items.Select(x => x.Rating), CharacterAdvancement.AttributeDistribution).Length == 0;
    public string InitialAttributesDescription => IsNpc
        ? "NPC: distribua livremente os grupos Físicos, Sociais e Mentais, de 0 a 5."
        : IsAttributeBaseLocked
            ? "Base registrada. Cada ponto novo custa o nível alcançado × 5 XP: 1→2 = 10 XP, 2→3 = 15, 3→4 = 20, 4→5 = 25. Baixar um atributo abaixo da base libera a redistribuição."
            : IsAttributeSpreadCustom
                ? $"Sem base registrada, nada é cobrado. Para cobrar XP, deixe exatamente {DotSpread(CharacterAdvancement.AttributeDistribution)} e depois suba."
                : $"Monte a base: {DotSpread(CharacterAdvancement.AttributeDistribution)}. Ela é registrada assim que a distribuição fecha; depois disso, cada ponto extra custa XP.";
    public string AttributesSummary
    {
        get
        {
            if (IsAttributeSpreadCustom) return "Distribuição personalizada — atributos não geram XP.";
            if (!IsAttributeBaseLocked) return InitialAttributesRemaining;
            var spent = _experienceCalculation?.Costs.Where(x => x.Section == SheetSection.Attributes).Sum(x => x.Cost) ?? 0;
            return spent == 0 ? "Nenhum XP gasto em atributos." : $"Total em atributos: {spent} XP.";
        }
    }
    private void SyncAttributeBase()
    {
        if (IsNpc || _advancement is null) return;
        _advancement = CharacterAdvancement.SyncAttributeBase(_advancement, Attributes.Items.Select(x => x.ToValue()));
    }
    private void SyncDisciplineBase()
    {
        if (IsNpc || _advancement is null) return;
        _advancement = CharacterAdvancement.SyncDisciplineBase(_advancement, Snapshot().Details, Disciplines.Items.Select(x => x.ToValue()));
    }
    private void RefreshAttributeInfo()
    {
        var bases = _advancement?.AttributeBase.ToDictionary(x => x.Key, x => x.Rating) ?? [];
        foreach (var attribute in Attributes.Items)
        {
            if (!bases.TryGetValue(attribute.Key, out var baseRating))
            {
                attribute.AdvancementInfo = "";
                continue;
            }
            var spent = _experienceCalculation?.Costs.FirstOrDefault(x => x.Section == SheetSection.Attributes && x.Name == attribute.Name)?.Cost ?? 0;
            var next = attribute.Rating >= 5 ? "" : attribute.Rating < baseRating
                ? "\npróximo sem custo" : $"\npróximo +{(attribute.Rating + 1) * 5} XP";
            attribute.AdvancementInfo = spent > 0 ? $"base {baseRating} · {spent} XP{next}" : $"base {baseRating}{next}";
        }
    }
    public string InitialSkillsDescription =>
        $"{DotSpread(CharacterAdvancement.SkillDistribution((SkillProfile)SkillProfileIndex))} · demais em 0";
    public bool IsInitialSkillsComplete => !CharacterAdvancement.MissingDistribution(Skills.Items.Select(x => x.Rating),
        CharacterAdvancement.SkillDistribution((SkillProfile)SkillProfileIndex)).Any();
    public const string SkillProfileHelp =
        "Especialista: 4 · 3 · 3 · 3 · 2 · 2 · 2 · 1 · 1 · 1\nEquilibrado: 3 · 3 · 3 · 2 · 2 · 2 · 2 · 2 · 1 · 1 · 1 · 1 · 1 · 1\n" +
        "Pau para toda obra: 3 · 2 · 2 · 2 · 2 · 2 · 2 · 2 · 2 · 1 · 1 · 1 · 1 · 1 · 1 · 1 · 1 · 1 · 1\n\n" +
        "Distribua esses pontos gratuitos; as demais habilidades ficam em 0. Uma nota maior cobre uma vaga menor.";
    public string InitialAttributesRemaining => MissingRatings(Attributes.Items.Select(x => x.Rating),
        CharacterAdvancement.AttributeDistribution);
    public string InitialSkillsRemaining => MissingRatings(Skills.Items.Select(x => x.Rating),
        CharacterAdvancement.SkillDistribution((SkillProfile)SkillProfileIndex));
    public string InitialSkillsStatus
    {
        get
        {
            var missing = CharacterAdvancement.MissingDistribution(Skills.Items.Select(x => x.Rating),
                CharacterAdvancement.SkillDistribution((SkillProfile)SkillProfileIndex)).ToArray();
            return missing.Length == 0 ? "✓ completa" : $"Faltam: {DotSpread(missing)}";
        }
    }
    public static readonly int[] DisciplineDistribution = CharacterAdvancement.DisciplineDistribution;
    public string InitialDisciplinesSpread => $"{DotSpread(DisciplineDistribution)} do clã · +1 × ● do predador";
    public const string DisciplineHelp =
        "1 × ●● disciplina principal do clã\n1 × ● disciplina secundária do clã\n1 × ● do tipo de predador (aplicado na seção Vantagens)\n\n" +
        "Acima disso, cada novo nível custa nível × 5 XP (do clã) ou × 7 XP (fora do clã).";
    private int[] MissingDisciplines()
    {
        var predatorKey = _predatorSelection?.DisciplineKey;
        var ratings = Disciplines.Items
            .Where(x => DisciplineAdvancement.IsInClan(Clan, x.Name) == true)
            .Select(x => x.Key == predatorKey ? x.Rating - 1 : x.Rating);
        return CharacterAdvancement.MissingDistribution(ratings, DisciplineDistribution);
    }
    public bool IsInitialDisciplinesComplete => MissingDisciplines().Length == 0;
    public string InitialDisciplinesStatus => MissingDisciplines() is { Length: > 0 } missing ? $"Faltam: {DotSpread(missing)}" : "✓ completa";
    public int DisciplinesExperienceTotal => _experienceCalculation?.Costs.Where(x => x.Section == SheetSection.Disciplines).Sum(x => x.Cost) ?? 0;
    public string DisciplinesExperienceHelp =>
        "Cada nível acima dos pontos iniciais custa o novo nível × 5 XP (do clã) ou × 7 XP (fora do clã).\n\n" +
        (string.IsNullOrEmpty(DisciplinesExperience) ? "Nenhum XP gasto em disciplinas." : DisciplinesExperience);
    /// <summary>"1 × ●●●● · 3 × ●●● · 1 × ●": how many slots of each rating, highest first.</summary>
    public static string DotSpread(IEnumerable<int> ratings) => string.Join(" · ", ratings.Where(x => x > 0)
        .GroupBy(x => x).OrderByDescending(x => x.Key).Select(x => $"{x.Count()} × {new string('●', x.Key)}"));
    public string InitialAdvantagesDescription =>
        $"Pontos iniciais gratuitos: {CharacterAdvancement.AdvantageBudget((VampireAge)AgeIndex)} em vantagens (inclui loresheets). " +
        $"Desvantagens de referencia: {CharacterAdvancement.FlawBudget((VampireAge)AgeIndex)}. Vantagens excedentes: 3 XP por ponto; bonus de predador nao sao cobrados.";
    public string AdvantageSelectionGuidance
    {
        get
        {
            if (IsNpc) return "";
            var traits = Snapshot().Traits;
            var budget = CharacterAdvancement.AdvantageBudget((VampireAge)AgeIndex);
            var selected = CharacterAdvancement.AdvantagePoints(traits);
            var status = selected < budget ? $"faltam {budget - selected}"
                : selected > budget ? $"excedente {selected - budget} × 3 = {(selected - budget) * 3} XP" : "✓ completos";
            var xp = _experienceCalculation?.Costs.Where(x => x.Section == SheetSection.Advantages).Sum(x => x.Cost) ?? 0;
            return $"Base da idade: {budget} pontos · Usados: {selected} · {status}" +
                (xp > 0 ? $" · XP em vantagens: {xp}" : "") + ". Cada ponto acima da base custa 3 XP; os do predador são gratuitos.";
        }
    }
    public string FlawSelectionGuidance
    {
        get
        {
            if (IsNpc) return "";
            var budget = CharacterAdvancement.FlawBudget((VampireAge)AgeIndex);
            var selected = CharacterAdvancement.FlawPoints(Snapshot().Traits);
            return $"Base da idade: {budget} pontos · Usados: {selected} · " +
                (selected < budget ? $"faltam {budget - selected}" : "✓ completos") +
                ". Defeitos não concedem XP; os do predador são adicionais.";
        }
    }    public int FreeSpecializationsAvailable
    {
        get
        {
            var snapshot = Snapshot();
            return CharacterAdvancement.FreeSpecializationsRemaining(snapshot.Details, snapshot.Traits);
        }
    }
    public const string SpecializationHelp =
        "Escreva uma especialização por linha (Enter) ou separe por ponto e vírgula.\n\n" +
        "Gratuitas: 1 por ficha; +1 em cada habilidade que chega a 4; +1 no primeiro ponto de " +
        "Erudição, Ofícios, Performance e Ciência. A do predador é adicional e aplicada automaticamente.\n\n" +
        "Cada especialização além das gratuitas custa 3 XP.";
    public int SkillsExperienceTotal => _experienceCalculation?.Costs.Where(x => x.Section == SheetSection.Skills).Sum(x => x.Cost) ?? 0;
    public string SkillsExperienceHelp =>
        "Cada ponto acima da distribuição inicial custa o novo nível × 3 XP (ex.: 2→3 = 9 XP).\n\n" +
        (string.IsNullOrEmpty(SkillsExperience) ? "Nenhum XP gasto em habilidades." : SkillsExperience);
    public string FreeSpecializationSummary =>
        $"Especializacoes gratuitas disponiveis: {FreeSpecializationsAvailable}. " +
        "1 por ficha; a especializacao escolhida no predador e adicional e aplicada automaticamente. +1 local ao atingir 4; " +
        "+1 local no primeiro ponto de Erudicao, Oficios, Performance e Ciencia. " +
        "Separe uma especializacao por linha ou por ponto e virgula. Excedentes: 3 XP cada, calculados automaticamente.";
    public int InitialExperience => CharacterAdvancement.StartingXp((VampireAge)AgeIndex);
    public int MaximumCampaignExperience => 1000000 - InitialExperience;
    public string ExperienceWarning => SectionExperience(SheetSection.Background);
    public string AttributesExperience => SectionExperience(SheetSection.Attributes);
    public string SkillsExperience => SectionExperience(SheetSection.Skills);
    public string DisciplinesExperience => SectionExperience(SheetSection.Disciplines);
    public string AdvantagesExperience => SectionExperience(SheetSection.Advantages);
    public string StateExperience => SectionExperience(SheetSection.State);
    public string[] ExperienceBreakdown => _experienceCalculation?.Costs.Select(x => $"{x.Name}: {x.Cost} XP.").ToArray() ?? [];
    private string SectionExperience(string section) => _experienceCalculation is not { } calculation ? "" :
        string.Join(Environment.NewLine, calculation.Costs.Where(x => x.Section == section).Select(x => $"{x.Name}: {x.Cost} XP.")
            .Concat(calculation.Warnings.Where(x => x.Field == section).Select(x => x.Message)));
    private static string MissingRatings(IEnumerable<int> ratings, IEnumerable<int> expected)
    {
        var missing = CharacterAdvancement.MissingDistribution(ratings, expected);
        return missing.Length == 0 ? "Distribuicao inicial completa." : $"Faltam: {DotSpread(missing)}";
    }
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
                nameof(InitialAdvantagesDescription), nameof(AdvantageSelectionGuidance), nameof(FlawSelectionGuidance),
                nameof(ExperienceBreakdown), nameof(AttributesSummary), nameof(SkillsExperienceTotal), nameof(SkillsExperienceHelp),
                nameof(FreeSpecializationsAvailable), nameof(DisciplinesExperienceTotal), nameof(DisciplinesExperienceHelp),
                nameof(InitialDisciplinesStatus), nameof(IsInitialDisciplinesComplete), nameof(InitialSkillsStatus),
                nameof(IsInitialSkillsComplete), nameof(InitialSkillsRemaining), nameof(InitialAttributesRemaining) })
                OnPropertyChanged(property);
            RefreshAttributeInfo();
            RefreshLoresheetSummary();
        }
        finally { _ready = ready; }
    }
    private void RefreshCreation()
    {
        foreach (var property in new[] { nameof(IsCreating), nameof(CanEditBuild), nameof(AgeIndex),
            nameof(SkillProfileIndex), nameof(InitialSkillsDescription), nameof(InitialAttributesRemaining),
            nameof(InitialSkillsRemaining), nameof(InitialSkillsStatus), nameof(IsInitialSkillsComplete), nameof(FreeSpecializationSummary),
            nameof(FreeSpecializationsAvailable), nameof(GenerationGuidance),
            nameof(AdvantageSelectionGuidance), nameof(FlawSelectionGuidance),
            nameof(IsAttributeBaseLocked), nameof(InitialAttributesDescription), nameof(AttributesSummary) })
            OnPropertyChanged(property);
        RefreshLoresheetSummary();
    }
    private void ReportAdvancementError(InvalidOperationException exception)
    {
        _logger.LogWarning(exception, "Operacao da ficha recusada para {CharacterId}", Id);
        Error = exception.Message;
    }
}
