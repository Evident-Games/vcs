namespace VCS.Domain.Characters;

public sealed class AdvantageDefinition
{
    public string Id { get; set; } = "";
    public string Group { get; set; } = "";
    public string Name { get; set; } = "";
    public TraitCategory Category { get; set; }
    public int Minimum { get; set; }
    public int Maximum { get; set; }
    public string PointsLabel { get; set; } = "";
    public string Description { get; set; } = "";
    public string LoresheetCollection { get; set; } = "";
    public bool IsLineage { get; set; }
}
