namespace VCS.Infrastructure.Persistence;

/// <summary>A user clan, or the user's edit of a built-in clan (same id). Lists are stored as JSON arrays.</summary>
public sealed class ClanCatalogRecord
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Disciplines { get; set; } = "[]";
    public string Aliases { get; set; } = "[]";
    public string Bane { get; set; } = "";
}
