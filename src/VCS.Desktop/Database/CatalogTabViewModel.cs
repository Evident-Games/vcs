using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VCS.Desktop.Database;

/// <summary>A row in a catalog list; Badge tells whether it came with the app, was edited, or was added by the user.</summary>
public sealed record CatalogRow(string Id, string Title, string Subtitle, bool IsBuiltIn, bool IsModified, object Value)
{
    public string Badge => !IsBuiltIn ? "seu" : IsModified ? "editado" : "";
}

/// <summary>One tab of the database editor: a searchable list on the left and the selected entry's form on the right.</summary>
public abstract partial class CatalogTabViewModel : ObservableObject
{
    protected CatalogTabViewModel(string title)
    {
        Title = title;
        var view = new ListCollectionView(Rows) { Filter = Matches };
        VisibleRows = view;
    }

    public string Title { get; }
    public ObservableCollection<CatalogRow> Rows { get; } = [];
    public ICollectionView VisibleRows { get; }
    [ObservableProperty] private string searchText = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete), nameof(CanReset))]
    private CatalogRow? selectedRow;
    [ObservableProperty] private object? draft;
    [ObservableProperty] private string error = "";
    [ObservableProperty] private string status = "";
    public bool CanDelete => SelectedRow is { IsBuiltIn: false };
    public bool CanReset => SelectedRow is { IsBuiltIn: true, IsModified: true };

    partial void OnSearchTextChanged(string value) => VisibleRows.Refresh();

    partial void OnSelectedRowChanged(CatalogRow? value)
    {
        Error = "";
        if (value is not null) Draft = CreateDraft(value);
    }

    public async Task LoadAsync(string? selectId = null)
    {
        Rows.Clear();
        foreach (var row in await ListAsync()) Rows.Add(row);
        SelectedRow = Rows.FirstOrDefault(x => x.Id == selectId) ?? Rows.FirstOrDefault();
    }

    [RelayCommand]
    private void New()
    {
        SelectedRow = null;
        Error = "";
        Status = "";
        Draft = CreateDraft(null);
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        if (Draft is null) return;
        var id = await SaveDraftAsync(Draft, SelectedRow);
        await LoadAsync(id);
        Status = "Salvo. Fichas abertas usam a mudanca ao serem reabertas.";
    });

    [RelayCommand]
    private Task DeleteAsync() => RunAsync(async () =>
    {
        if (SelectedRow is not { IsBuiltIn: false } row) return;
        await DeleteAsync(row.Id);
        await LoadAsync();
        Status = $"{row.Title} excluido.";
    });

    [RelayCommand]
    private Task ResetAsync() => RunAsync(async () =>
    {
        if (SelectedRow is not { IsBuiltIn: true } row) return;
        await ResetAsync(row.Id);
        await LoadAsync(row.Id);
        Status = $"{row.Title} voltou ao padrao do aplicativo.";
    });

    protected abstract Task<IReadOnlyList<CatalogRow>> ListAsync();
    protected abstract object CreateDraft(CatalogRow? row);
    /// <summary>Saves the form and returns the saved entry's id.</summary>
    protected abstract Task<string> SaveDraftAsync(object draft, CatalogRow? editing);
    protected abstract Task DeleteAsync(string id);
    protected abstract Task ResetAsync(string id);

    private async Task RunAsync(Func<Task> action)
    {
        Error = "";
        Status = "";
        try { await action(); }
        catch (InvalidOperationException exception) { Error = exception.Message; }
    }

    private bool Matches(object item)
    {
        var term = SearchText.Trim();
        return term.Length == 0 || item is CatalogRow row &&
            (row.Title.Contains(term, StringComparison.CurrentCultureIgnoreCase) || row.Subtitle.Contains(term, StringComparison.CurrentCultureIgnoreCase));
    }
}
