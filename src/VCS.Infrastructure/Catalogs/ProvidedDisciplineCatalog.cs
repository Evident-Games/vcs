using VCS.Application.Catalogs;
using VCS.Content.Disciplines;

namespace VCS.Infrastructure.Catalogs;

/// <summary>The powers in use: the built-in ones from <see cref="DisciplineCatalog"/> with the user's database edits applied.</summary>
public sealed class ProvidedDisciplineCatalog : IDisciplineCatalog
{
    /// <summary>Powers in use: built-in ones with user edits applied, then user powers.</summary>
    public IReadOnlyList<PowerDefinition> Powers { get; private set; } = DisciplineCatalog.Powers;
    public IReadOnlyList<PowerDefinition> BuiltIn => DisciplineCatalog.Powers;

    public void Apply(IEnumerable<PowerDefinition> edits)
    {
        var byId = edits.ToDictionary(x => x.Id);
        var builtInIds = BuiltIn.Select(x => x.Id).ToHashSet();
        Powers = Array.AsReadOnly([.. BuiltIn.Select(x => byId.GetValueOrDefault(x.Id, x)), .. byId.Values.Where(x => !builtInIds.Contains(x.Id))]);
    }
}
