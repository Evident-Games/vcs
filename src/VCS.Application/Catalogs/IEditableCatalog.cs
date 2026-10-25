namespace VCS.Application.Catalogs;

/// <summary>A catalog entry with where it came from: shipped with the app (built in) and whether the user changed it.</summary>
public sealed record CatalogEntry<T>(string Id, T Value, bool IsBuiltIn, bool IsModified);

/// <summary>
/// A catalog the user can edit from the database editor. Built-in entries can be changed and reset, not deleted,
/// because existing sheets use them; entries the user added can also be deleted.
/// </summary>
public interface IEditableCatalog<T>
{
    Task<IReadOnlyList<CatalogEntry<T>>> ListEntriesAsync(CancellationToken cancellationToken = default);
    /// <summary>Adds or updates an entry; <paramref name="replacingId"/> is the old id when a user entry was renamed.</summary>
    Task SaveAsync(T value, string? replacingId = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task ResetAsync(string id, CancellationToken cancellationToken = default);
    /// <summary>Applies stored edits to catalogs the running app keeps in memory (clans, discipline powers).</summary>
    Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
