using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Domain.Characters;

namespace VCS.Desktop.Sheet;

/// <summary>One note in the notes list; editing it refreshes its date.</summary>
public sealed partial class NoteViewModel(CharacterNote note) : ObservableObject
{
    public Guid Id { get; } = note.Id;
    [ObservableProperty] private string title = note.Title;
    [ObservableProperty] private string text = note.Text;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DateLabel))]
    private DateTime updatedUtc = note.UpdatedUtc;
    public string DateLabel => UpdatedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    public string Preview => Text.Split('\n', 2)[0].Trim();

    partial void OnTitleChanged(string value) => UpdatedUtc = DateTime.UtcNow;

    partial void OnTextChanged(string value)
    {
        UpdatedUtc = DateTime.UtcNow;
        OnPropertyChanged(nameof(Preview));
    }

    public CharacterNote ToValue() => new(Id, Title, Text, UpdatedUtc);
}
