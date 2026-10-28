namespace VCS.Infrastructure.Persistence;

/// <summary>A user discipline power or ritual, or the user's edit of a built-in one (same id).</summary>
public sealed class PowerCatalogRecord
{
    public string Id { get; set; } = "";
    public string DisciplineId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public string Cost { get; set; } = "";
    public string DicePool { get; set; } = "";
    public string Summary { get; set; } = "";
    public string? AmalgamDisciplineId { get; set; }
    public int AmalgamLevel { get; set; }
    public bool IsRitual { get; set; }
}
