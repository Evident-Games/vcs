using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Application.Catalogs;

namespace VCS.Desktop.Database;

public sealed partial class PowerDraft(PowerDefinition power) : ObservableObject, ICatalogDraft<PowerDefinition>
{
    [ObservableProperty] private string discipline = power.Discipline;
    [ObservableProperty] private string name = power.Name;
    [ObservableProperty] private int level = power.Level;
    [ObservableProperty] private string cost = power.Cost;
    [ObservableProperty] private string dicePool = power.DicePool;
    [ObservableProperty] private string summary = power.Summary;
    [ObservableProperty] private string amalgamDiscipline = power.AmalgamDiscipline ?? "";
    [ObservableProperty] private int amalgamLevel = power.AmalgamLevel;

    public PowerDefinition ToValue() => power with
    {
        Discipline = Discipline.Trim(), Name = Name.Trim(), Level = Level, Cost = Cost.Trim(), DicePool = DicePool.Trim(),
        Summary = Summary.Trim(), AmalgamDiscipline = string.IsNullOrWhiteSpace(AmalgamDiscipline) ? null : AmalgamDiscipline.Trim(),
        AmalgamLevel = string.IsNullOrWhiteSpace(AmalgamDiscipline) ? 0 : AmalgamLevel
    };
}
