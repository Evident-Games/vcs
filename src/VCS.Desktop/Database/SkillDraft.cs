using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Domain.Skills;

namespace VCS.Desktop.Database;

public sealed partial class SkillDraft(SkillDefinition skill) : ObservableObject, ICatalogDraft<SkillDefinition>
{
    public static string[] Groups { get; } = ["Físicas", "Sociais", "Mentais"];
    [ObservableProperty] private string name = skill.Name;
    [ObservableProperty] private string group = skill.Group;
    [ObservableProperty] private string description = skill.Description;
    public SkillDefinition ToValue() => skill with { Name = Name.Trim(), Group = Group.Trim(), Description = Description };
}
