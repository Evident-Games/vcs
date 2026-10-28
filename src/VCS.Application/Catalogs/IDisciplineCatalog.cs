using VCS.Content.Disciplines;

namespace VCS.Application.Catalogs;

public interface IDisciplineCatalog
{
    IReadOnlyList<PowerDefinition> Powers { get; }
}
