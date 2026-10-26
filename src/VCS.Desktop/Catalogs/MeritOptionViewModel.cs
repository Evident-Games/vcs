using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Content.Advantages;
using VCS.Sheet;

namespace VCS.Desktop.Catalogs;

public sealed class MeritOptionViewModel(MeritFamilyViewModel family, AdvantageDefinition definition) : ObservableObject
{
    public AdvantageDefinition Definition { get; } = definition;
    public string Name => family.Find(Definition)?.Name ?? Definition.Name;
    public string Kind => Definition.Category == TraitCategory.Flaw ? "Defeito" : "Vantagem";
    public string Description => $"{Definition.PointsLabel} ponto(s). {Definition.Description}";
    public int Minimum => Definition.Minimum;
    public int Maximum => Definition.Maximum;
    public bool IsSelected
    {
        get => family.Find(Definition) is not null;
        set
        {
            if (value != IsSelected) family.SetOption(Definition, value ? Definition.Minimum : 0);
        }
    }
    public int Rating
    {
        get => family.Find(Definition)?.Rating ?? 0;
        set => family.SetOption(Definition, value);
    }
    public string Warning => family.Find(Definition) is { } item &&
        (item.Rating < Minimum || item.Rating > Maximum) ? "Pontos antigos fora da referencia; revise esta entrada." : "";
    internal void Refresh()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(IsSelected));
        OnPropertyChanged(nameof(Rating));
        OnPropertyChanged(nameof(Warning));
    }
}
