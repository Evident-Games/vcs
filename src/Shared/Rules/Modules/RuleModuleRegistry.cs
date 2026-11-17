namespace VCS.Rules.Modules;

/// <summary>Distinguishes a full edition ruleset from an optional rules/content extension.</summary>
public enum RuleModuleKind
{
    Edition,
    Extension
}

/// <summary>
/// Declarative compatibility contract for an edition ruleset or optional extension.
/// Compatibility is opt-in; similarity of names or mechanics grants no compatibility.
/// </summary>
public sealed record RuleModuleManifest(
    string ModuleId,
    string Version,
    RuleModuleKind Kind,
    string EditionId,
    IReadOnlyCollection<string> CompatibleEditionIds,
    IReadOnlyCollection<string> RequiredModuleIds,
    IReadOnlyCollection<string> ConflictingModuleIds);

/// <summary>The validated module set selected for one character/chronicle edition.</summary>
public sealed record RuleModuleSelection(
    string EditionId,
    IReadOnlyList<RuleModuleManifest> Modules);

/// <summary>
/// Resolves module selections without loading rules by reflection or assuming cross-edition compatibility.
/// This registry is infrastructure only; the application must explicitly compose it before using it.
/// </summary>
public sealed class RuleModuleRegistry
{
    private readonly IReadOnlyDictionary<string, RuleModuleManifest> _modules;

    public RuleModuleRegistry(IEnumerable<RuleModuleManifest> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);

        var entries = modules.ToArray();
        foreach (var module in entries)
            ValidateManifest(module);

        _modules = entries.ToDictionary(module => module.ModuleId, StringComparer.OrdinalIgnoreCase);
    }

    public RuleModuleSelection Resolve(string editionId, IEnumerable<string>? enabledModuleIds = null)
    {
        if (string.IsNullOrWhiteSpace(editionId))
            throw new ArgumentException("An edition identifier is required.", nameof(editionId));

        var requested = (enabledModuleIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .ToArray();

        var selectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in requested)
        {
            if (!selectedIds.Add(id))
                throw new InvalidOperationException($"Module '{id}' was selected more than once.");
        }

        var editionModules = _modules.Values
            .Where(module => module.Kind == RuleModuleKind.Edition &&
                string.Equals(module.EditionId, editionId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (editionModules.Length != 1)
            throw new NotSupportedException(
                editionModules.Length == 0
                    ? $"No rules module is registered for edition '{editionId}'."
                    : $"More than one rules module is registered for edition '{editionId}'.");

        selectedIds.Add(editionModules[0].ModuleId);

        var selected = selectedIds.Select(id =>
            _modules.TryGetValue(id, out var module)
                ? module
                : throw new NotSupportedException($"Module '{id}' is not installed."))
            .ToArray();

        foreach (var module in selected)
        {
            if (module.Kind == RuleModuleKind.Edition)
            {
                if (!string.Equals(module.EditionId, editionId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"Edition module '{module.ModuleId}' does not match '{editionId}'.");
            }
            else if (!module.CompatibleEditionIds.Contains(editionId, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Extension '{module.ModuleId}' has not declared compatibility with '{editionId}'.");
            }

            foreach (var requiredId in module.RequiredModuleIds)
            {
                if (!_modules.ContainsKey(requiredId))
                    throw new InvalidOperationException(
                        $"Module '{module.ModuleId}' requires missing module '{requiredId}'.");
                if (!selectedIds.Contains(requiredId))
                    throw new InvalidOperationException(
                        $"Module '{module.ModuleId}' requires '{requiredId}' to be selected.");
            }

            foreach (var conflictingId in module.ConflictingModuleIds)
            {
                if (!_modules.ContainsKey(conflictingId))
                    throw new InvalidOperationException(
                        $"Module '{module.ModuleId}' declares unknown conflict '{conflictingId}'.");
                if (selectedIds.Contains(conflictingId))
                    throw new InvalidOperationException(
                        $"Modules '{module.ModuleId}' and '{conflictingId}' cannot be enabled together.");
            }
        }

        var ordered = selected
            .OrderBy(module => module.Kind == RuleModuleKind.Edition ? 0 : 1)
            .ThenBy(module => module.ModuleId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new RuleModuleSelection(editionId, Array.AsReadOnly(ordered));
    }

    private static void ValidateManifest(RuleModuleManifest module)
    {
        ArgumentNullException.ThrowIfNull(module);
        if (string.IsNullOrWhiteSpace(module.ModuleId))
            throw new ArgumentException("Every rule module needs a stable identifier.", nameof(module));
        if (string.IsNullOrWhiteSpace(module.Version))
            throw new ArgumentException($"Module '{module.ModuleId}' needs a version.", nameof(module));
        if (string.IsNullOrWhiteSpace(module.EditionId))
            throw new ArgumentException($"Module '{module.ModuleId}' needs a base edition identifier.", nameof(module));
        ArgumentNullException.ThrowIfNull(module.CompatibleEditionIds);
        ArgumentNullException.ThrowIfNull(module.RequiredModuleIds);
        ArgumentNullException.ThrowIfNull(module.ConflictingModuleIds);

        if (module.Kind == RuleModuleKind.Extension && module.CompatibleEditionIds.Count == 0)
            throw new ArgumentException(
                $"Extension '{module.ModuleId}' must declare every compatible edition explicitly.", nameof(module));
    }
}
