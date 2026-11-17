using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Content.Disciplines;

namespace VCS.Desktop.Database;

/// <summary>Discipline form; the app's own disciplines are shown read only.</summary>
public sealed partial class DisciplineDraft(DisciplineDefinition discipline, bool isBuiltIn) : ObservableObject, ICatalogDraft<DisciplineDefinition>
{
    public bool CanRename => !isBuiltIn;
    [ObservableProperty] private string name = discipline.Name;

    public DisciplineDefinition ToValue() => discipline with { Name = Name.Trim() };
}
