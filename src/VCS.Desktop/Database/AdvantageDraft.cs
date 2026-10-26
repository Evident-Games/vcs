using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Content.Advantages;
using VCS.Sheet;

namespace VCS.Desktop.Database;

public sealed partial class AdvantageDraft(AdvantageDefinition advantage) : ObservableObject, ICatalogDraft<AdvantageDefinition>
{
    public static TraitCategory[] Categories { get; } = [TraitCategory.Advantage, TraitCategory.Flaw, TraitCategory.Loresheet];
    [ObservableProperty] private string name = advantage.Name;
    [ObservableProperty] private string group = advantage.Group;
    [ObservableProperty] private TraitCategory category = advantage.Category;
    [ObservableProperty] private int minimum = advantage.Minimum;
    [ObservableProperty] private int maximum = advantage.Maximum;
    [ObservableProperty] private string pointsLabel = advantage.PointsLabel;
    [ObservableProperty] private string description = advantage.Description;
    [ObservableProperty] private string loresheetCollection = advantage.LoresheetCollection;
    [ObservableProperty] private bool isLineage = advantage.IsLineage;

    public AdvantageDefinition ToValue() => new()
    {
        Id = advantage.Id, Name = Name.Trim(), Group = Group.Trim(), Category = Category, Minimum = Minimum, Maximum = Maximum,
        PointsLabel = PointsLabel, Description = Description, LoresheetCollection = LoresheetCollection.Trim(), IsLineage = IsLineage
    };
}
