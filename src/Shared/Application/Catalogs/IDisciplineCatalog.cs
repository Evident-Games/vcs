using Game.Content.Disciplines;

namespace Game.Application.Catalogs;

public interface IDisciplineCatalog
{
    IReadOnlyList<PowerDefinition> Powers { get; }
}
