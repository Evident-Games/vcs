using VCS.Application.Catalogs;

namespace VCS.Desktop.Database;

/// <summary>Editable form of one catalog entry.</summary>
public interface ICatalogDraft<out T>
{
    T ToValue();
}

/// <summary>Database editor tab for one catalog type; describe and toDraft adapt the entry to the list and the form.</summary>
public sealed class CatalogTab<T, TDraft>(string title, IEditableCatalog<T> catalog,
    Func<T, (string Title, string Subtitle)> describe, Func<CatalogEntry<T>?, TDraft> toDraft) : CatalogTabViewModel(title)
    where TDraft : ICatalogDraft<T>
{
    protected override async Task<IReadOnlyList<CatalogRow>> ListAsync() =>
        [.. (await catalog.ListEntriesAsync()).Select(x =>
        {
            var (rowTitle, subtitle) = describe(x.Value);
            return new CatalogRow(x.Id, rowTitle, subtitle, x.IsBuiltIn, x.IsModified, x);
        })];

    protected override object CreateDraft(CatalogRow? row) => toDraft((CatalogEntry<T>?)row?.Value)!;

    protected override async Task<string> SaveDraftAsync(object draft, CatalogRow? editing)
    {
        var value = ((TDraft)draft).ToValue();
        await catalog.SaveAsync(value, editing?.Id);
        // New entries get their id on save and a renamed clan changes id; find the saved one again by its title.
        var title = describe(value).Title;
        var entries = await catalog.ListEntriesAsync();
        return entries.FirstOrDefault(x => x.Id == editing?.Id && describe(x.Value).Title == title)?.Id
            ?? entries.LastOrDefault(x => describe(x.Value).Title == title)?.Id ?? "";
    }

    protected override Task DeleteAsync(string id) => catalog.DeleteAsync(id);
    protected override Task ResetAsync(string id) => catalog.ResetAsync(id);
}
