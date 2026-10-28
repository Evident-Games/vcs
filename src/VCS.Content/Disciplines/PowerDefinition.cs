using System.Text.Json.Serialization;

namespace VCS.Content.Disciplines;

/// <summary>A discipline power, or a ritual (<see cref="IsRitual"/>) of the discipline that performs it.</summary>
public sealed record PowerDefinition(
    string Id, string DisciplineId, string Name, int Level, string Cost, string DicePool, string Summary,
    string? AmalgamDisciplineId = null, int AmalgamLevel = 0, bool IsRitual = false)
{
    [JsonIgnore] public string BaseDescription => "Descricao base da disciplina nao fornecida.";
    [JsonIgnore] public string Ingredients => "Nao informado no material fornecido.";
    /// <summary>Name of the discipline, as shown.</summary>
    [JsonIgnore] public string Discipline => DisciplineCatalog.NameOf(DisciplineId);
    [JsonIgnore] public string? AmalgamDiscipline => AmalgamDisciplineId is null ? null : DisciplineCatalog.NameOf(AmalgamDisciplineId);
}
