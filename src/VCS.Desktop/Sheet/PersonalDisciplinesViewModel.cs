using System.Collections.ObjectModel;
using VCS.Domain.Characters;

namespace VCS.Desktop.Sheet;

/// <summary>
/// Disciplines the player wrote in (not from the catalog or the predator), shown as name + dots lines
/// with an empty draft line at the end, like personal skills.
/// </summary>
public sealed class PersonalDisciplinesViewModel : IPersonalTraitOwner
{
    private readonly TraitGroupViewModel _disciplines;

    public PersonalDisciplinesViewModel(TraitGroupViewModel disciplines)
    {
        _disciplines = disciplines;
        foreach (var item in disciplines.Items.Where(IsPersonal)) Lines.Add(new(this, item));
        Lines.Add(new(this));
    }

    public ObservableCollection<PersonalTraitLineViewModel> Lines { get; } = [];

    /// <summary>Catalog disciplines use "disc-" keys and predator ones "pred-disc-"; anything else was written by the player.</summary>
    public static bool IsPersonal(TraitItemViewModel item) =>
        !item.Key.StartsWith("disc-", StringComparison.Ordinal) && !item.Key.StartsWith("pred-disc-", StringComparison.Ordinal);

    public void Materialize(PersonalTraitLineViewModel draft)
    {
        if (draft.Trait is not null || string.IsNullOrWhiteSpace(draft.Name)) return;
        _disciplines.AddEntry(new(TraitCategory.Discipline, Guid.NewGuid().ToString("N"), draft.Name.Trim(), draft.Rating));
        draft.Attach(_disciplines.Items.Last());
        Lines.Add(new(this));
    }

    public void RemoveIfEmpty(PersonalTraitLineViewModel line)
    {
        if (line.Trait is not { } item || !string.IsNullOrWhiteSpace(item.Name)) return;
        _disciplines.RemoveCommand.Execute(item);
        Lines.Remove(line);
        if (Lines.All(x => !x.IsDraft)) Lines.Add(new(this));
    }
}
