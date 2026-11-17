namespace VCS.Infrastructure.Persistence;

/// <summary>A user discipline, or the user's edit of a built-in one (same id).</summary>
public sealed class DisciplineCatalogRecord
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}
