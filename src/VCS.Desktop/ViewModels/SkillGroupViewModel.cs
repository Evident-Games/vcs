using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Domain.Characters;

namespace VCS.Desktop.ViewModels;

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

public partial class SkillEntryLineViewModel : ObservableObject
{
    private readonly SkillGroupViewModel _group;
    private TraitItemViewModel? _trait;
    private string name = "";
    private string specialization = "";
    private int rating;

    internal SkillEntryLineViewModel(SkillGroupViewModel group, TraitItemViewModel? trait = null)
    {
        _group = group;
        Attach(trait);
    }

    internal TraitItemViewModel? Trait => _trait;
    public bool IsDraft => _trait is null;
    public int SpecializationCount => CharacterAdvancement.SpecializationCount(Specialization);
    public string Name
    {
        get => _trait?.Name ?? name;
        set
        {
            if (_trait is { } trait)
            {
                if (trait.CanEditName) trait.Name = value;
                OnPropertyChanged(nameof(Name));
                _group.RemoveIfEmpty(this);
                return;
            }
            if (name == value) return;
            name = value;
            OnPropertyChanged();
            _group.Materialize(this);
        }
    }
    public string Specialization
    {
        get => _trait?.Specialization ?? specialization;
        set
        {
            if (_trait is { } trait) trait.Specialization = value;
            else specialization = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SpecializationCount));
            if (_trait is null) _group.Materialize(this);
            else _group.RemoveIfEmpty(this);
        }
    }
    public int Rating
    {
        get => _trait?.Rating ?? rating;
        set
        {
            if (_trait is { } trait) trait.Rating = value;
            else rating = value;
            OnPropertyChanged();
            if (_trait is null) _group.Materialize(this);
        }
    }

    internal void Attach(TraitItemViewModel? trait)
    {
        _trait = trait;
        if (trait is not null)
        {
            name = trait.Name;
            specialization = trait.Specialization;
            rating = trait.Rating;
        }
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Specialization));
        OnPropertyChanged(nameof(SpecializationCount));
        OnPropertyChanged(nameof(Rating));
        OnPropertyChanged(nameof(IsDraft));
    }
}
