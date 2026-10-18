namespace VCS.Desktop.ViewModels;

public sealed record SkillGroupViewModel(string Name, IReadOnlyList<TraitItemViewModel> Items);
