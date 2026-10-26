namespace VCS.Content.Disciplines;

public sealed record PowerDefinition(
    string Id, string Discipline, string Name, int Level, string Cost, string DicePool, string Summary,
    string? AmalgamDiscipline = null, int AmalgamLevel = 0)
{
    public string BaseDescription => "Descricao base da disciplina nao fornecida.";
    public string Ingredients => "Nao informado no material fornecido.";
    public bool IsRitual => Discipline == "Rituais";
    public string RequiredDiscipline => IsRitual ? "Feitiçaria de Sangue" : Discipline;
}
