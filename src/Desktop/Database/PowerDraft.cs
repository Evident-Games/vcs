using CommunityToolkit.Mvvm.ComponentModel;
using Game.Content.Disciplines;

namespace Game.Desktop.Database;

/// <summary>Power form; the discipline and the amalgam are picked from the disciplines in use.</summary>
public sealed partial class PowerDraft(PowerDefinition power) : ObservableObject, ICatalogDraft<PowerDefinition>
{
    /// <summary>The disciplines to pick from; the amalgam list starts with "none".</summary>
    public IReadOnlyList<DisciplineDefinition> Disciplines { get; } = DisciplineCatalog.All;
    public IReadOnlyList<DisciplineDefinition> AmalgamOptions { get; } = [new("", "(nenhum)"), .. DisciplineCatalog.All];

    [ObservableProperty] private string disciplineId = power.DisciplineId;
    [ObservableProperty] private bool isRitual = power.IsRitual;
    [ObservableProperty] private string name = power.Name;
    [ObservableProperty] private int level = power.Level;
    [ObservableProperty] private string cost = power.Cost;
    [ObservableProperty] private string dicePool = power.DicePool;
    [ObservableProperty] private string summary = power.Summary;
    [ObservableProperty] private string amalgamDisciplineId = power.AmalgamDisciplineId ?? "";
    [ObservableProperty] private int amalgamLevel = power.AmalgamLevel;
    [ObservableProperty] private string prerequisite = power.Prerequisite;
    [ObservableProperty] private string source = power.Source;

    public PowerDefinition ToValue() => power with
    {
        DisciplineId = DisciplineId, IsRitual = IsRitual, Name = Name.Trim(), Level = Level, Cost = Cost.Trim(), DicePool = DicePool.Trim(),
        Summary = Summary.Trim(), AmalgamDisciplineId = AmalgamDisciplineId.Length == 0 ? null : AmalgamDisciplineId,
        AmalgamLevel = AmalgamDisciplineId.Length == 0 ? 0 : AmalgamLevel,
        Prerequisite = Prerequisite.Trim(), Source = Source.Trim()
    };
}
