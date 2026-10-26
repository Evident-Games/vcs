using System.Text.Json.Serialization;

namespace VCS.Content.Disciplines;

public sealed record PowerDefinition(
    string Id, string Discipline, string Name, int Level, string Cost, string DicePool, string Summary,
    string? AmalgamDiscipline = null, int AmalgamLevel = 0)
{
    [JsonIgnore] public string BaseDescription => "Descricao base da disciplina nao fornecida.";
    [JsonIgnore] public string Ingredients => "Nao informado no material fornecido.";
    [JsonIgnore] public bool IsRitual => Discipline == "Rituais";
    [JsonIgnore] public string RequiredDiscipline => IsRitual ? "Feitiçaria de Sangue" : Discipline;
}
