using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Controls;
using VCS.Desktop.ViewModels;

namespace VCS.Desktop.Views;

public partial class SkillsView : UserControl
{
    private CharacterEditorViewModel? _editor;
    private readonly List<SkillGroupViewModel> _watched = [];

    public SkillsView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Attach(DataContext as CharacterEditorViewModel);
        Unloaded += (_, _) => Attach(null);
        Loaded += (_, _) => Attach(DataContext as CharacterEditorViewModel);
    }

    /// <summary>Skills laid out row by row so each row's height is shared by the three columns.</summary>
    public ObservableCollection<SkillGridRow> Rows { get; } = [];

    private void Attach(CharacterEditorViewModel? editor)
    {
        if (ReferenceEquals(editor, _editor) && (editor is null || Rows.Count > 0)) return;
        if (_editor is not null) _editor.SkillGroups.CollectionChanged -= OnGroupsChanged;
        _editor = editor;
        if (_editor is not null) _editor.SkillGroups.CollectionChanged += OnGroupsChanged;
        Rebuild();
    }

    private void OnGroupsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();
    private void OnPersonalChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        foreach (var group in _watched) group.PersonalItems.CollectionChanged -= OnPersonalChanged;
        _watched.Clear();
        if (_editor is null) { Rows.Clear(); return; }
        var groups = _editor.SkillGroups.Take(3).ToArray();
        foreach (var group in groups)
        {
            group.PersonalItems.CollectionChanged += OnPersonalChanged;
            _watched.Add(group);
        }
        var columns = groups.Select(g => g.Items.Cast<object>().Concat(g.PersonalItems).ToArray()).ToArray();
        var count = columns.Length == 0 ? 0 : columns.Max(x => x.Length);
        // Only replace rows whose cells changed so editors keep focus while drafts are added.
        for (var row = 0; row < count; row++)
        {
            var next = new SkillGridRow(Cell(columns, 0, row), Cell(columns, 1, row), Cell(columns, 2, row));
            if (row >= Rows.Count) Rows.Add(next);
            else if (Rows[row] != next) Rows[row] = next;
        }
        while (Rows.Count > count) Rows.RemoveAt(Rows.Count - 1);
    }

    private static object? Cell(object[][] columns, int column, int row) =>
        column < columns.Length && row < columns[column].Length ? columns[column][row] : null;
}

public sealed record SkillGridRow(object? Physical, object? Social, object? Mental);