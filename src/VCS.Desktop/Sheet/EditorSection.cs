using VCS.Desktop.Layout;

namespace VCS.Desktop.Sheet;

/// <summary>A section shown in one sheet; the option carries its live title, tab and bento size.</summary>
public sealed record EditorSection(SectionOption Option, CharacterEditorViewModel Editor)
{
    public string Key => Option.Key;
    public string Title => Option.DisplayTitle;
}

/// <summary>A tab of a sheet in tab mode.</summary>
public sealed record EditorTab(string Title, IReadOnlyList<EditorSection> Sections);
