using System.Globalization;

namespace VCS.Content.Clans;

public sealed record ClanDefinition(
    string Id, string Name, string Description, IReadOnlyList<string> Disciplines, IReadOnlyList<string> Aliases,
    string Bane = "");

public static class ClanCatalog
{
    /// <summary>
    /// Clans in use: the built-in ones, with user edits (same id) replacing them and user clans added after.
    /// Set at startup and after each edit from the database editor.
    /// </summary>
    // ponytail: process-wide catalog read by the rules; fine for a single-user desktop app, a per-call catalog if that changes.
    private static IReadOnlyList<ClanDefinition>? _configured;
    public static IReadOnlyList<ClanDefinition> All => _configured ?? BuiltIn;

    public static void Configure(IEnumerable<ClanDefinition> custom) => _configured = Merge(custom);

    /// <summary>The built-in clans with <paramref name="custom"/> edits applied by id, then the user clans.</summary>
    public static IReadOnlyList<ClanDefinition> Merge(IEnumerable<ClanDefinition> custom)
    {
        var edits = custom.ToArray();
        return Array.AsReadOnly([.. BuiltIn.Select(x => edits.FirstOrDefault(edit => edit.Id == x.Id) ?? x),
            .. edits.Where(edit => !IsBuiltIn(edit.Id))]);
    }

    public static bool IsBuiltIn(string id) => BuiltIn.Any(x => x.Id == id);

    public static IReadOnlyList<ClanDefinition> BuiltIn { get; } = BuiltInContent.Load<ClanDefinition>("clans.json");

    public static bool SameName(string first, string second) => CultureInfo.InvariantCulture.CompareInfo.Compare(
        first.Trim(), second.Trim(), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0;

    public static ClanDefinition? Find(string name) => Find(All, name);

    public static ClanDefinition? Find(IEnumerable<ClanDefinition> clans, string name) =>
        clans.FirstOrDefault(x => SameName(x.Name, name) || x.Aliases.Any(alias => SameName(alias, name)));
}
