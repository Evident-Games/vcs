namespace VCS.Infrastructure.Persistence;

/// <summary>A user discipline power, or the user's edit of a built-in power (same id).</summary>
public sealed class PowerCatalogRecord
{
    public string Id { get; set; } = "";
    public string Discipline { get; set; } = "";
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public string Cost { get; set; } = "";
    public string DicePool { get; set; } = "";
    public string Summary { get; set; } = "";
    public string? AmalgamDiscipline { get; set; }
    public int AmalgamLevel { get; set; }
}
