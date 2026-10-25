using System.Collections.ObjectModel;
using VCS.Domain.Characters;

namespace VCS.Desktop.Sheet;

public sealed partial class SkillGroupViewModel
{
    private readonly TraitGroupViewModel _skills;
    public SkillGroupViewModel(string name, IReadOnlyList<TraitItemViewModel> items,
        IEnumerable<TraitItemViewModel> personal, TraitGroupViewModel skills)
    {
        Name = name;
        Items = items;
        _skills = skills;
        foreach (var item in personal)
        {
            item.SkillGroup ??= name;
            PersonalItems.Add(new(this, item));
        }
        PersonalItems.Add(new(this));
    }

    public string Name { get; }
    public IReadOnlyList<TraitItemViewModel> Items { get; }
    public ObservableCollection<SkillEntryLineViewModel> PersonalItems { get; } = [];

    internal void Materialize(SkillEntryLineViewModel draft)
    {
        if (draft.Trait is not null ||
            string.IsNullOrWhiteSpace(draft.Name) && string.IsNullOrWhiteSpace(draft.Specialization)) return;
        _skills.AddEntry(new(TraitCategory.Skill, Guid.NewGuid().ToString("N"), draft.Name.Trim(),
            draft.Rating, draft.Specialization, Skill: new(Name)));
        draft.Attach(_skills.Items.Last());
        PersonalItems.Add(new(this));
    }

    internal void RemoveIfEmpty(SkillEntryLineViewModel line)
    {
        if (line.Trait is not { } item ||
            !string.IsNullOrWhiteSpace(item.Name) || !string.IsNullOrWhiteSpace(item.Specialization)) return;
        _skills.RemoveCommand.Execute(item);
        PersonalItems.Remove(line);
        if (PersonalItems.All(x => !x.IsDraft)) PersonalItems.Add(new(this));
    }
}
