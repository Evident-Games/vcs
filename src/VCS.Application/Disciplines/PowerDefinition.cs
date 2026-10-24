using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace VCS.Application.Disciplines;

public sealed record PowerDefinition(
    string Id, string Discipline, string Name, int Level, string Cost, string DicePool, string Summary,
    string? AmalgamDiscipline = null, int AmalgamLevel = 0)
{
    public string BaseDescription => "Descricao base da disciplina nao fornecida.";
    public string Ingredients => "Nao informado no material fornecido.";
    public bool IsRitual => Discipline == "Rituais";
    public string RequiredDiscipline => IsRitual ? "Feitiçaria de Sangue" : Discipline;
}

public interface IDisciplineCatalog
{
    IReadOnlyList<PowerDefinition> Powers { get; }
}

public static class CatalogText
{
    public static string Normalize(string value) => string.Concat(value.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
        .Normalize(NormalizationForm.FormC).Trim().ToUpperInvariant();

    public static string StableId(string prefix, string value) =>
        prefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(value))));
}
