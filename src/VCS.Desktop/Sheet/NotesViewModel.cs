using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Sheet;

namespace VCS.Desktop.Sheet;

/// <summary>Titled notes of a sheet: a searchable list on the left, the selected note's title and text on the right.</summary>
public sealed partial class NotesViewModel : ObservableObject
{
    public NotesViewModel(IEnumerable<CharacterNote> notes)
    {
        foreach (var note in notes.OrderByDescending(x => x.UpdatedUtc)) Attach(new NoteViewModel(note));
        var view = new ListCollectionView(Notes) { Filter = Matches, IsLiveSorting = true };
        view.SortDescriptions.Add(new(nameof(NoteViewModel.UpdatedUtc), ListSortDirection.Descending));
        view.LiveSortingProperties.Add(nameof(NoteViewModel.UpdatedUtc));
        VisibleNotes = view;
        selectedNote = Notes.FirstOrDefault();
    }

    public ObservableCollection<NoteViewModel> Notes { get; } = [];
    public ICollectionView VisibleNotes { get; }
    [ObservableProperty] private NoteViewModel? selectedNote;
    [ObservableProperty] private string searchText = "";
    public bool HasNotes => Notes.Count != 0;
    public event EventHandler? Changed;

    public IReadOnlyList<CharacterNote> ToValues() => [.. Notes.Select(x => x.ToValue())];

    partial void OnSearchTextChanged(string value) => VisibleNotes.Refresh();

    [RelayCommand]
    private void Add()
    {
        SearchText = "";
        var note = new NoteViewModel(new(Guid.NewGuid(), "Nova nota", "", DateTime.UtcNow));
        Attach(note);
        SelectedNote = note;
        RaiseChanged();
    }

    [RelayCommand]
    private void Delete(NoteViewModel? note)
    {
        note ??= SelectedNote;
        if (note is null || !Notes.Remove(note)) return;
        note.PropertyChanged -= OnNoteChanged;
        if (ReferenceEquals(SelectedNote, note)) SelectedNote = VisibleNotes.Cast<NoteViewModel>().FirstOrDefault();
        RaiseChanged();
    }

    private void Attach(NoteViewModel note)
    {
        note.PropertyChanged += OnNoteChanged;
        Notes.Add(note);
    }

    private void OnNoteChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(NoteViewModel.Title) or nameof(NoteViewModel.Text)) Changed?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseChanged()
    {
        OnPropertyChanged(nameof(HasNotes));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private bool Matches(object item)
    {
        var term = SearchText.Trim();
        return term.Length == 0 || item is NoteViewModel note &&
            (note.Title.Contains(term, StringComparison.OrdinalIgnoreCase) || note.Text.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}
