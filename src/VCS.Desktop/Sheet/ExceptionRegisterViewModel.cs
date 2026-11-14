using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Sheet;

namespace VCS.Desktop.Sheet;

/// <summary>Editable, character-specific log of Storyteller-approved departures from normal requirements.</summary>
public sealed partial class ExceptionRegisterViewModel : ObservableObject
{
    public ExceptionRegisterViewModel(IEnumerable<SheetException> exceptions)
    {
        foreach (var exception in exceptions.OrderByDescending(x => x.ApprovedUtc))
        {
            var entry = new SheetExceptionViewModel(exception);
            Attach(entry);
            Entries.Add(entry);
        }
    }

    public ObservableCollection<SheetExceptionViewModel> Entries { get; } = [];
    public int Count => Entries.Count;
    public event EventHandler? Changed;

    public IReadOnlyList<SheetException> ToValues() => [.. Entries.Select(x => x.ToValue())];

    [RelayCommand]
    private void Add()
    {
        var entry = new SheetExceptionViewModel(new(Guid.NewGuid(), "Vantagem", "", "", "Aprovado pelo Narrador", "Narrador", DateTime.UtcNow));
        entry.PropertyChanged += OnEntryChanged;
        Entries.Insert(0, entry);
        OnPropertyChanged(nameof(Count));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Remove(SheetExceptionViewModel? entry)
    {
        if (entry is null || !Entries.Remove(entry)) return;
        entry.PropertyChanged -= OnEntryChanged;
        OnPropertyChanged(nameof(Count));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Attach(SheetExceptionViewModel entry)
    {
        entry.PropertyChanged += OnEntryChanged;
    }

    private void OnEntryChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SheetExceptionViewModel.Area) or nameof(SheetExceptionViewModel.Subject) or
            nameof(SheetExceptionViewModel.RuleOrPrerequisite) or nameof(SheetExceptionViewModel.Ruling) or
            nameof(SheetExceptionViewModel.AuthorizedBy))
            Changed?.Invoke(this, EventArgs.Empty);
    }
}

public sealed partial class SheetExceptionViewModel(SheetException value) : ObservableObject
{
    public Guid Id { get; } = value.Id;
    public DateTime ApprovedUtc { get; } = value.ApprovedUtc;
    public string ApprovedDate => ApprovedUtc.ToLocalTime().ToString("dd/MM/yyyy");

    [ObservableProperty] private string area = value.Area;
    [ObservableProperty] private string subject = value.Subject;
    [ObservableProperty] private string ruleOrPrerequisite = value.RuleOrPrerequisite;
    [ObservableProperty] private string ruling = value.Ruling;
    [ObservableProperty] private string authorizedBy = value.AuthorizedBy;

    public SheetException ToValue() => new(Id, Area.Trim(), Subject.Trim(), RuleOrPrerequisite.Trim(),
        Ruling.Trim(), AuthorizedBy.Trim(), ApprovedUtc);
}
