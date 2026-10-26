using VCS.Content.Advantages;
using VCS.Desktop.Sheet;
using VCS.Sheet;

namespace VCS.Desktop.Catalogs;

/// <summary>Sheet services the advantage rows need: predator allocation and loresheet purchases.</summary>
public interface IMeritSheetHost
{
    TraitGroupViewModel Loresheets { get; }
    /// <summary>The variable predator option behind a granted line, or null when the line is fixed.</summary>
    PredatorAllocationViewModel? PredatorOption(TraitItemViewModel grant);
    IReadOnlyList<PredatorAllocationViewModel> PendingPredatorOptions(TraitCategory category);
    int PredatorRemaining(TraitCategory category);
    int PredatorRoom(PredatorAllocationViewModel option);
    void AllocatePredator(PredatorAllocationViewModel option, int points);
    /// <summary>Buys the benefit; returns the refusal message or null.</summary>
    string? AddLoresheet(AdvantageDefinition benefit);
    void RemoveLoresheet(TraitItemViewModel item);
}
