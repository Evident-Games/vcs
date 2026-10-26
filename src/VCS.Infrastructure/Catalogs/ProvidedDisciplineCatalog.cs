using System.Globalization;
using System.Text.RegularExpressions;
using VCS.Application.Catalogs;
using VCS.Content.Disciplines;

namespace VCS.Infrastructure.Catalogs;

public sealed partial class ProvidedDisciplineCatalog : IDisciplineCatalog
{
    public ProvidedDisciplineCatalog()
    {
        using var stream = typeof(ProvidedDisciplineCatalog).Assembly.GetManifestResourceStream(
            "VCS.Infrastructure.Catalogs.Seed.Disciplines.txt")
            ?? throw new InvalidDataException("O catalogo de disciplinas nao foi incluido no aplicativo.");
        using var reader = new StreamReader(stream);
        Powers = BuiltIn = Parse(reader.ReadToEnd());
    }

    /// <summary>Powers in use: built-in ones with user edits applied, then user powers.</summary>
    public IReadOnlyList<PowerDefinition> Powers { get; private set; }
    public IReadOnlyList<PowerDefinition> BuiltIn { get; }

    public void Apply(IEnumerable<PowerDefinition> edits)
    {
        var byId = edits.ToDictionary(x => x.Id);
        var builtInIds = BuiltIn.Select(x => x.Id).ToHashSet();
        Powers = Array.AsReadOnly([.. BuiltIn.Select(x => byId.GetValueOrDefault(x.Id, x)), .. byId.Values.Where(x => !builtInIds.Contains(x.Id))]);
    }

    public static IReadOnlyList<PowerDefinition> Parse(string text)
    {
        var matches = EntryPattern().Matches(text);
        var headers = Regex.Matches(text, @"^Disciplina:", RegexOptions.Multiline).Count;
        if (headers == 0 || matches.Count != headers)
            throw new InvalidDataException("O catalogo possui uma entrada incompleta ou malformada.");
        var powers = matches.Select(match =>
        {
            var discipline = match.Groups["discipline"].Value.Trim();
            var name = match.Groups["name"].Value.Trim();
            return new PowerDefinition(match.Groups["id"].Value,
                discipline, name, int.Parse(match.Groups["level"].Value, CultureInfo.InvariantCulture),
                match.Groups["cost"].Value.Trim(), match.Groups["pool"].Value.Trim(), match.Groups["summary"].Value.Trim(),
                match.Groups["amalgam"].Success ? match.Groups["amalgam"].Value.Trim() : null,
                match.Groups["amalgamLevel"].Success ? int.Parse(match.Groups["amalgamLevel"].Value, CultureInfo.InvariantCulture) : 0);
        }).ToArray();
        if (powers.Any(x => x.Level is < 1 or > 5 || x.Name.Length is < 1 or > 80 ||
                string.IsNullOrWhiteSpace(x.Cost) || string.IsNullOrWhiteSpace(x.DicePool) || string.IsNullOrWhiteSpace(x.Summary)) ||
            powers.Any(x => !x.Id.StartsWith(x.IsRitual ? "ritual-" : "power-", StringComparison.Ordinal)) ||
            powers.Select(x => x.Id).Distinct().Count() != powers.Length)
            throw new InvalidDataException("O catalogo possui valores invalidos ou poderes duplicados.");
        return Array.AsReadOnly(powers);
    }

    [GeneratedRegex(@"^Id: (?<id>[a-z0-9-]+)\r?\nDisciplina: (?<discipline>[^\r\n]+)\r?\nNome da habilidade: (?<name>[^\r\n]+) \(Nível (?<level>[0-9]+)(?: - Amálgama: (?<amalgam>[^\r\n]+) (?<amalgamLevel>[1-5]))?\)\r?\nCusto: (?<cost>[^\r\n]+)\r?\nParada de dados: (?<pool>[^\r\n]+)\r?\nResumo: (?<summary>[^\r\n]+)", RegexOptions.Multiline)]
    private static partial Regex EntryPattern();
}
