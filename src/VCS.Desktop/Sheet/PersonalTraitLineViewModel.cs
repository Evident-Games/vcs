using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Rules.V5.Advancement;

namespace VCS.Desktop.Sheet;

/// <summary>Owner of personal trait lines: turns a typed draft into a trait and drops a line whose name was cleared.</summary>
public interface IPersonalTraitOwner
{
    void Materialize(PersonalTraitLineViewModel draft);
    void RemoveIfEmpty(PersonalTraitLineViewModel line);
}

/// <summary>A personal skill or discipline line; the last line of a list is an empty draft that becomes a trait when typed in.</summary>
public partial class PersonalTraitLineViewModel : ObservableObject
{
    private readonly IPersonalTraitOwner _group;
    private TraitItemViewModel? _trait;
    private string name = "";
    private string specialization = "";
    private int rating;

    internal PersonalTraitLineViewModel(IPersonalTraitOwner group, TraitItemViewModel? trait = null)
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
