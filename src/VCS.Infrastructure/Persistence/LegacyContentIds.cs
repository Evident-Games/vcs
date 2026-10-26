using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VCS.Infrastructure.Catalogs;
using VCS.Sheet;

namespace VCS.Infrastructure.Persistence;

/// <summary>
/// Rewrites content ids saved before every id carried its kind in English (databases before the ContentIds
/// migrations, JSON files before format 13). The tables describe old data, so they never change with the catalog.
/// </summary>
internal static partial class LegacyContentIds
{
    /// <summary>Migrations after which saved sheets still hold old ids; the upgrade is idempotent, so each one reruns it.</summary>
    internal static readonly string[] Migrations =
        ["20261026192826_ContentIdsAdvantagesAndPredators", "20261026194137_ContentIdsAttributes"];

    private static readonly Dictionary<string, string> Advantages = new(StringComparer.Ordinal)
    {
        ["catalog-visual-belo"] = "advantage-looks-beautiful",
        ["catalog-deslumbrante"] = "advantage-looks-stunning",
        ["catalog-feio"] = "flaw-looks-ugly",
        ["catalog-repulsivo"] = "flaw-looks-repulsive",
        ["catalog-refugio"] = "advantage-haven",
        ["catalog-arsenal-oculto"] = "advantage-haven-hidden-armory",
        ["catalog-vigilancia"] = "advantage-haven-watchmen",
        ["catalog-luxo"] = "advantage-haven-luxury",
        ["catalog-nenhum-refugio"] = "flaw-no-haven",
        ["catalog-assombrado"] = "flaw-haven-haunted",
        ["catalog-comprometido"] = "flaw-haven-compromised",
        ["catalog-recursos"] = "advantage-resources",
        ["catalog-destituido"] = "flaw-destitute",
        ["catalog-sabujo-de-sangue"] = "advantage-bloodhound",
        ["catalog-esofago-de-ferro"] = "advantage-iron-gullet",
        ["catalog-exclusao-de-presa"] = "flaw-prey-exclusion",
        ["catalog-sede-do-matusalem"] = "flaw-methuselahs-thirst",
        ["catalog-fazendeiro"] = "flaw-farmer",
        ["catalog-organivoro"] = "flaw-organovore",
        ["catalog-mawla"] = "advantage-mawla",
        ["catalog-status"] = "advantage-status",
        ["catalog-adversario"] = "flaw-adversary",
        ["catalog-suspeito"] = "flaw-suspect",
        ["catalog-segregado"] = "flaw-shunned",
        ["catalog-vivendo-no-passado"] = "flaw-living-in-the-past",
        ["catalog-arcaico"] = "flaw-archaic",
        ["catalog-ingerir-comida"] = "advantage-eat-food",
        ["catalog-perdicao-folclorica"] = "flaw-folkloric-bane",
        ["catalog-tabu-folclorico"] = "flaw-folkloric-block",
        ["catalog-estigma"] = "flaw-stigmata",
        ["catalog-isca-de-estaca"] = "flaw-stake-bait",
        ["catalog-mascara"] = "advantage-mask",
        ["catalog-cadaver-identificado"] = "flaw-known-corpse",
        ["catalog-corpo-frio-identificado"] = "flaw-known-blankbody",
        ["catalog-linguistica"] = "advantage-linguistics",
        ["catalog-analfabeto"] = "flaw-illiterate",
        ["catalog-lacaios"] = "advantage-retainers",
        ["catalog-aliados"] = "advantage-allies",
        ["catalog-contatos"] = "advantage-contacts",
        ["catalog-rebanho"] = "advantage-herd",
        ["catalog-perseguidores"] = "flaw-stalkers",
        ["catalog-inimigo"] = "flaw-enemy",
        ["catalog-predador-manifesto"] = "flaw-obvious-predator",
    };

    private static readonly Dictionary<string, string> Loresheets = new(StringComparer.Ordinal)
    {
        ["bahari"] = "bahari", ["theo-bell"] = "theo-bell", ["heresia-cainita"] = "cainite-heresy", ["carna"] = "carna"
    };

    private static readonly Dictionary<string, string> Predators = new(StringComparer.Ordinal)
    {
        ["alleycat"] = "predator-alleycat", ["extortionist"] = "predator-extortionist", ["roadside"] = "predator-roadside",
        ["cleaver"] = "predator-cleaver", ["consensualist"] = "predator-consensualist", ["osiris"] = "predator-osiris",
        ["scene-queen"] = "predator-scene-queen", ["siren"] = "predator-siren", ["sandman"] = "predator-sandman",
        ["graverobber"] = "predator-graverobber", ["bagger"] = "predator-bagger", ["blood-leech"] = "predator-blood-leech",
        ["farmer"] = "predator-farmer"
    };

    public static CharacterDetails Upgrade(CharacterDetails details) => details with
    {
        PredatorSelection = Upgrade(details.PredatorSelection),
        Advancement = Upgrade(details.Advancement)
    };

    public static TraitValue Upgrade(TraitValue value) => value with { Key = Key(value.Category, value.Key) };

    private static readonly string[] Attributes =
        ["Strength", "Dexterity", "Stamina", "Charisma", "Manipulation", "Composure", "Intelligence", "Wits", "Resolve"];

    private static string Attribute(string key) => Attributes.Contains(key) ? "attribute-" + key.ToLowerInvariant() : key;

    public static string Predator(string id) => Predators.GetValueOrDefault(id, id);

    public static string Key(TraitCategory? category, string key)
    {
        if (category == TraitCategory.Attribute) return Attribute(key);
        if (category is not (TraitCategory.Advantage or TraitCategory.Flaw or TraitCategory.Loresheet)) return key;
        if (Advantages.TryGetValue(key, out var advantage)) return advantage;
        if (key.StartsWith("adv-custom-", StringComparison.Ordinal))
            return SqliteAdvantageCatalog.CustomIdPrefix(category.Value) + key["adv-custom-".Length..];
        if (LoresheetId().Match(key) is { Success: true } lore && Loresheets.TryGetValue(lore.Groups[1].Value, out var sheet))
            return $"loresheet-{sheet}-{lore.Groups[2].Value}";
        if (PredatorGrant().Match(key) is { Success: true } grant)
            return $"predator:{Predator(grant.Groups[1].Value)}:{grant.Groups[2].Value}";
        return key;
    }

    private static PredatorSelection? Upgrade(PredatorSelection? selection) => selection is null ? null : selection with
    {
        Id = Predator(selection.Id),
        Grants = selection.Grants?.Select(Upgrade).ToArray()!
    };

    private static AdvancementState? Upgrade(AdvancementState? state) => state is null ? null : state with
    {
        Baseline = [.. state.Baseline.Select(Upgrade)],
        Purchases = [.. state.Purchases.Select(x => x with { Key = Key(x.Category, x.Key) })],
        AttributeBase = [.. state.AttributeBase.Select(x => x with { Key = Attribute(x.Key) })],
        BaselinePredatorSelection = Upgrade(state.BaselinePredatorSelection)
    };

    /// <summary>
    /// Upgrades every saved sheet in place. Uses SQL instead of the entities so revisions and update times stay as they were.
    /// </summary>
    public static async Task UpgradeAsync(VcsDbContext db, CancellationToken cancellationToken)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var connection = db.Database.GetDbConnection();
        var traits = new List<(string Id, TraitCategory Category, string Key)>();
        var details = new List<(string Id, string? Predator, string? Advancement)>();
        await using (var read = connection.CreateCommand())
        {
            read.CommandText = "SELECT \"Id\", \"Category\", \"Key\" FROM \"CharacterTraits\"";
            await using var rows = await read.ExecuteReaderAsync(cancellationToken);
            while (await rows.ReadAsync(cancellationToken))
                traits.Add((rows.GetString(0), (TraitCategory)rows.GetInt32(1), rows.GetString(2)));
        }
        await using (var read = connection.CreateCommand())
        {
            read.CommandText = "SELECT \"Id\", \"Details_PredatorSelection\", \"Details_Advancement\" FROM \"Characters\"";
            await using var rows = await read.ExecuteReaderAsync(cancellationToken);
            while (await rows.ReadAsync(cancellationToken))
                details.Add((rows.GetString(0), rows.IsDBNull(1) ? null : rows.GetString(1), rows.IsDBNull(2) ? null : rows.GetString(2)));
        }
        foreach (var (id, category, key) in traits)
            if (Key(category, key) is var upgraded && upgraded != key)
                await db.Database.ExecuteSqlAsync($"UPDATE \"CharacterTraits\" SET \"Key\" = {upgraded} WHERE \"Id\" = {id}", cancellationToken);
        foreach (var (id, predator, advancement) in details)
        {
            var newPredator = Rewrite<PredatorSelection>(predator, Upgrade);
            var newAdvancement = Rewrite<AdvancementState>(advancement, Upgrade);
            if (newPredator != predator || newAdvancement != advancement)
                await db.Database.ExecuteSqlAsync(
                    $"UPDATE \"Characters\" SET \"Details_PredatorSelection\" = {newPredator}, \"Details_Advancement\" = {newAdvancement} WHERE \"Id\" = {id}",
                    cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    // Same (default) serializer options as the column conversions in VcsDbContext.
    private static string? Rewrite<T>(string? json, Func<T?, T?> upgrade) where T : class
    {
        if (json is null) return null;
        var value = JsonSerializer.Deserialize<T>(json);
        var upgraded = upgrade(value);
        return Equals(value, upgraded) ? json : JsonSerializer.Serialize(upgraded);
    }

    [GeneratedRegex(@"^lore-(.+)-([1-5])$")]
    private static partial Regex LoresheetId();

    [GeneratedRegex(@"^predator:(.+):([0-9]+)$")]
    private static partial Regex PredatorGrant();
}
