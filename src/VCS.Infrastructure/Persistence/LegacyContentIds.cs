using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VCS.Content.Clans;
using VCS.Infrastructure.Catalogs;
using VCS.Sheet;

namespace VCS.Infrastructure.Persistence;

/// <summary>
/// Rewrites content ids saved before every id carried its kind in English (databases before the ContentIds
/// migrations, JSON files before format 13). The tables describe old data, so they never change with the catalog.
/// </summary>
internal sealed partial class LegacyContentIds
{
    /// <summary>Migrations after which saved sheets still hold old ids; the upgrade is idempotent, so each one reruns it.</summary>
    internal static readonly string[] Migrations =
    [
        "20261026192826_ContentIdsAdvantagesAndPredators", "20261026194137_ContentIdsAttributes",
        "20261026194910_ContentIdsDisciplinesAndPowers", "20261026195623_ContentIdsClans"
    ];

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

    private static readonly string[] Attributes =
        ["Strength", "Dexterity", "Stamina", "Charisma", "Manipulation", "Composure", "Intelligence", "Wits", "Resolve"];

    /// <summary>Old discipline keys were "disc-" plus the hash of the pt-BR name.</summary>
    private static readonly Dictionary<string, string> Disciplines = new (string Name, string Id)[]
    {
        ("Animalismo", "discipline-animalism"), ("Auspícios", "discipline-auspex"), ("Feitiçaria de Sangue", "discipline-blood-sorcery"),
        ("Celeridade", "discipline-celerity"), ("Dominação", "discipline-dominate"), ("Fortitude", "discipline-fortitude"),
        ("Ofuscação", "discipline-obfuscate"), ("Oblívio", "discipline-oblivion"), ("Potência", "discipline-potence"),
        ("Presença", "discipline-presence"), ("Proteanismo", "discipline-protean")
    }.ToDictionary(x => OldStableId("disc-", x.Name), x => x.Id, StringComparer.Ordinal);

    /// <summary>Old built-in power ids were "power-" plus the hash of the pt-BR discipline and name.</summary>
    internal static readonly IReadOnlyDictionary<string, string> Powers = new (string Discipline, string Name, string Id)[]
    {
        ("Animalismo", "Laço com Familiar", "power-animalism-bond-famulus"),
        ("Animalismo", "Sentir a Besta", "power-animalism-sense-the-beast"),
        ("Animalismo", "Sussurros Selvagens", "power-animalism-feral-whispers"),
        ("Animalismo", "Acalmar a Besta", "power-animalism-quell-the-beast"),
        ("Animalismo", "Suculência Animal", "power-animalism-animal-succulence"),
        ("Animalismo", "Subjugar o Espírito", "power-animalism-subsume-the-spirit"),
        ("Animalismo", "Domínio Animal", "power-animalism-animal-dominion"),
        ("Animalismo", "Extrair a Besta", "power-animalism-drawing-out-the-beast"),
        ("Auspícios", "Sentidos Aguçados", "power-auspex-heightened-senses"),
        ("Auspícios", "Sentir o Invisível", "power-auspex-sense-the-unseen"),
        ("Auspícios", "Premonição", "power-auspex-premonition"),
        ("Auspícios", "Compartilhar os Sentidos", "power-auspex-share-the-senses"),
        ("Auspícios", "Sondar a Alma", "power-auspex-scry-the-soul"),
        ("Auspícios", "O Toque do Espírito", "power-auspex-spirits-touch"),
        ("Auspícios", "Clarividência", "power-auspex-clairvoyance"),
        ("Auspícios", "Telepatia", "power-auspex-telepathy"),
        ("Auspícios", "Obeah", "power-auspex-obeah"),
        ("Auspícios", "Aliviando a Alma Bestial", "power-auspex-unburdening-the-bestial-soul"),
        ("Celeridade", "Graça Felina", "power-celerity-cats-grace"),
        ("Celeridade", "Reflexos Rápidos", "power-celerity-rapid-reflexes"),
        ("Celeridade", "Rapidez", "power-celerity-fleetness"),
        ("Celeridade", "Piscar / Travessia", "power-celerity-blink-traversal"),
        ("Celeridade", "Mira Infalível", "power-celerity-unerring-aim"),
        ("Celeridade", "Fração de Segundos", "power-celerity-split-second"),
        ("Dominação", "Compelir", "power-dominate-compel"),
        ("Dominação", "Obscurecer Memória", "power-dominate-cloud-memory"),
        ("Dominação", "Sugestão Hipnótica", "power-dominate-mesmerize"),
        ("Dominação", "O Esquece-me", "power-dominate-the-forgetful-mind"),
        ("Dominação", "Submersão da Diretriz", "power-dominate-submerged-directive"),
        ("Dominação", "Racionalização", "power-dominate-rationalize"),
        ("Dominação", "Comando em Massa", "power-dominate-mass-manipulation"),
        ("Dominação", "Ditame Terminal", "power-dominate-terminal-decree"),
        ("Fortitude", "Resiliência", "power-fortitude-resilience"),
        ("Fortitude", "Mente Inabalável", "power-fortitude-unswayable-mind"),
        ("Fortitude", "Resiliência das Bestas", "power-fortitude-enduring-beasts"),
        ("Fortitude", "Ignorar Danos", "power-fortitude-defy-bane"),
        ("Fortitude", "Fortificar Barreira Íntima", "power-fortitude-fortify-the-inner-facade"),
        ("Fortitude", "Proteção de Mármore", "power-fortitude-flesh-of-marble"),
        ("Fortitude", "Agonia da Carne", "power-fortitude-prowess-from-pain"),
        ("Ofuscação", "Manto de Sombras", "power-obfuscate-cloak-of-shadows"),
        ("Ofuscação", "Silêncio da Morte", "power-obfuscate-silence-of-death"),
        ("Ofuscação", "Passagem Invisível", "power-obfuscate-unseen-passage"),
        ("Ofuscação", "Máscara das Mil Faces", "power-obfuscate-mask-of-a-thousand-faces"),
        ("Ofuscação", "Fantasma Cibernético", "power-obfuscate-ghost-in-the-machine"),
        ("Ofuscação", "Ocultar", "power-obfuscate-conceal"),
        ("Ofuscação", "Manto do Grupo", "power-obfuscate-cloak-the-gathering"),
        ("Ofuscação", "Quimerismo", "power-obfuscate-chimerstry"),
        ("Ofuscação", "Fata Morgana", "power-obfuscate-fata-morgana"),
        ("Potência", "Salto Impressionante", "power-potence-soaring-leap"),
        ("Potência", "Corpo Letal", "power-potence-lethal-body"),
        ("Potência", "Bravura", "power-potence-prowess"),
        ("Potência", "Alimentação Brutal", "power-potence-brutal-feed"),
        ("Potência", "Poção de Força", "power-potence-draught-of-might"),
        ("Potência", "Choque Terrestre", "power-potence-earthshock"),
        ("Presença", "Olhar Aterrorizante", "power-presence-dread-gaze"),
        ("Presença", "Fascínio", "power-presence-awe"),
        ("Presença", "Beijo Persistente", "power-presence-lingering-kiss"),
        ("Presença", "Transe", "power-presence-entrancement"),
        ("Presença", "Convocação", "power-presence-summon"),
        ("Presença", "Majestade", "power-presence-majesty"),
        ("Proteanismo", "Peso da Pena", "power-protean-weight-of-the-feather"),
        ("Proteanismo", "Olhos da Besta", "power-protean-eyes-of-the-beast"),
        ("Proteanismo", "Armas da Fera", "power-protean-feral-weapons"),
        ("Proteanismo", "Fusão com a Terra", "power-protean-earth-meld"),
        ("Proteanismo", "Mudança de Forma", "power-protean-shapechange"),
        ("Proteanismo", "Forma de Névoa", "power-protean-mist-form"),
        ("Feitiçaria de Sangue", "Gosto de Sangue", "power-blood-sorcery-a-taste-for-blood"),
        ("Feitiçaria de Sangue", "Sangue Corrosivo", "power-blood-sorcery-corrosive-vitae"),
        ("Feitiçaria de Sangue", "Extinguir Vitae", "power-blood-sorcery-extinguish-vitae"),
        ("Feitiçaria de Sangue", "Toque do Escorpião", "power-blood-sorcery-scorpions-touch"),
        ("Feitiçaria de Sangue", "Roubo de Vitae", "power-blood-sorcery-theft-of-vitae"),
        ("Feitiçaria de Sangue", "Caldeirão de Sangue", "power-blood-sorcery-cauldron-of-blood"),
        ("Rituais", "Despertar com a Brisa do Entardecer", "ritual-wake-with-evenings-freshness"),
        ("Rituais", "Ilusão de Morte Pacífica", "ritual-illusion-of-peaceful-death"),
        ("Rituais", "Proteção contra Feras", "ritual-ward-against-lupines"),
        ("Rituais", "Proteção contra Cainitas", "ritual-ward-against-cainites"),
        ("Rituais", "Fuga para o Santuário Verdadeiro", "ritual-escape-to-true-sanctuary"),
        ("Oblívio", "Visão do Oblívio", "power-oblivion-oblivions-sight"),
        ("Oblívio", "Manto Sombrio", "power-oblivion-shadow-cloak"),
        ("Oblívio", "Braços de Ahriman", "power-oblivion-arms-of-ahriman"),
        ("Oblívio", "Toque do Oblívio", "power-oblivion-touch-of-oblivion"),
        ("Oblívio", "Mortalha Estígia", "power-oblivion-stygian-shroud"),
        ("Oblívio", "Passo Sombrio", "power-oblivion-shadow-step"),
        ("Oblívio", "Avatar Tenebroso", "power-oblivion-tenebrous-avatar")

    }.ToDictionary(x => OldStableId("power-", x.Discipline + "\n" + x.Name), x => x.Id, StringComparer.Ordinal);

    private readonly IReadOnlySet<string> _customRituals;
    private readonly IReadOnlyList<ClanDefinition> _clans;

    /// <param name="customRituals">Ids of rituals the user created ("ritual-custom-…"), once "power-custom-…".</param>
    /// <param name="clans">Clans to find a sheet's clan id by its name; the clans in use by default.</param>
    public LegacyContentIds(IReadOnlySet<string>? customRituals = null, IReadOnlyList<ClanDefinition>? clans = null)
    {
        _customRituals = customRituals ?? new HashSet<string>();
        _clans = clans ?? ClanCatalog.All;
    }

    /// <summary>Upgrades one sheet: its traits first, then everything that names them by key.</summary>
    public (CharacterDetails Details, TraitValue[] Traits) Upgrade(CharacterDetails details, IReadOnlyList<TraitValue> traits)
    {
        var upgraded = traits.Select(x => x with { Key = Key(x.Category, x.Key, x.Power?.IsRitual) }).ToArray();
        var renamed = traits.Zip(upgraded).Where(x => x.First.Key != x.Second.Key)
            .DistinctBy(x => (x.First.Category, x.First.Key))
            .ToDictionary(x => (x.First.Category, x.First.Key), x => x.Second.Key);
        string Rename(TraitCategory? category, string key) =>
            category is { } known && renamed.TryGetValue((known, key), out var to) ? to : Key(category, key, null);
        TraitValue RenameValue(TraitValue value) => value with { Key = Rename(value.Category, value.Key) };
        PredatorSelection? Selection(PredatorSelection? selection) => selection is null ? null : selection with
        {
            Id = Predator(selection.Id),
            DisciplineKey = selection.DisciplineKey is null ? null! : Rename(TraitCategory.Discipline, selection.DisciplineKey),
            SkillKey = selection.SkillKey is null ? null! : Rename(TraitCategory.Skill, selection.SkillKey),
            Grants = selection.Grants?.Select(RenameValue).ToArray()!
        };
        var advancement = details.Advancement is not { } state ? null : state with
        {
            Baseline = [.. state.Baseline.Select(RenameValue)],
            Purchases = [.. state.Purchases.Select(x => x with { Key = Rename(x.Category, x.Key) })],
            AttributeBase = [.. state.AttributeBase.Select(x => x with { Key = Rename(TraitCategory.Attribute, x.Key) })],
            DisciplineBase = [.. state.DisciplineBase.Select(x => x with { Key = Rename(TraitCategory.Discipline, x.Key) })],
            BaselinePredatorSelection = Selection(state.BaselinePredatorSelection)
        };
        var clanKey = details.ClanKey.Length > 0 ? details.ClanKey : ClanCatalog.Find(_clans, details.Clan)?.Id ?? "";
        return (details with { ClanKey = clanKey, PredatorSelection = Selection(details.PredatorSelection), Advancement = advancement }, upgraded);
    }

    public static string Predator(string id) => Predators.GetValueOrDefault(id, id);

    /// <param name="isRitual">From the sheet line's power details, when it has them.</param>
    private string Key(TraitCategory? category, string key, bool? isRitual)
    {
        if (category is not { } kind) return key;
        if (Guid32().IsMatch(key)) return $"{TraitKeys.Kind(kind, isRitual == true)}-personal-{key}";
        switch (kind)
        {
            case TraitCategory.Attribute:
                return Attributes.Contains(key) ? "attribute-" + key.ToLowerInvariant() : key;
            case TraitCategory.Discipline:
                if (Disciplines.TryGetValue(key, out var discipline)) return discipline;
                if (OldHash().Match(key) is { Success: true } custom && key.StartsWith("disc-", StringComparison.Ordinal))
                    return "discipline-custom-" + custom.Groups[1].Value[..32].ToLowerInvariant();
                return key.StartsWith("pred-disc-", StringComparison.Ordinal) ? "discipline-from-" + Predator(key["pred-disc-".Length..]) : key;
            case TraitCategory.DisciplinePower:
                if (Powers.TryGetValue(key, out var power)) return power;
                if (key.StartsWith("power-", StringComparison.Ordinal) && Guid32().IsMatch(key["power-".Length..]))
                    return $"{TraitKeys.Kind(kind, isRitual == true)}-personal-{key["power-".Length..]}";
                var ritual = key.Replace("power-custom-", "ritual-custom-", StringComparison.Ordinal);
                return ritual != key && _customRituals.Contains(ritual) ? ritual : key;
            case TraitCategory.Advantage or TraitCategory.Flaw or TraitCategory.Loresheet:
                if (Advantages.TryGetValue(key, out var advantage)) return advantage;
                if (key.StartsWith("adv-custom-", StringComparison.Ordinal))
                    return SqliteAdvantageCatalog.CustomIdPrefix(kind) + key["adv-custom-".Length..];
                if (LoresheetId().Match(key) is { Success: true } lore && Loresheets.TryGetValue(lore.Groups[1].Value, out var sheet))
                    return $"loresheet-{sheet}-{lore.Groups[2].Value}";
                return PredatorGrant().Match(key) is { Success: true } grant
                    ? $"predator:{Predator(grant.Groups[1].Value)}:{grant.Groups[2].Value}" : key;
            default:
                return key;
        }
    }

    /// <summary>
    /// Upgrades every saved sheet in place. Uses SQL instead of the entities so revisions and update times stay as they were.
    /// </summary>
    public static async Task UpgradeAsync(VcsDbContext db, CancellationToken cancellationToken)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var connection = db.Database.GetDbConnection();
        var rituals = await db.PowerCatalog.Where(x => x.Id.StartsWith("ritual-custom-")).Select(x => x.Id).ToListAsync(cancellationToken);
        // Sheets name their clan; user clans must be known to find its id.
        var clans = ClanCatalog.Merge(SqliteClanCatalog.Definitions(await db.ClanCatalog.AsNoTracking().ToListAsync(cancellationToken)));
        var converter = new LegacyContentIds(rituals.ToHashSet(), clans);
        var traits = new List<(string CharacterId, string Id, TraitValue Value)>();
        await using (var read = connection.CreateCommand())
        {
            read.CommandText = "SELECT \"CharacterId\", \"Id\", \"Category\", \"Key\", \"Power\" FROM \"CharacterTraits\"";
            await using var rows = await read.ExecuteReaderAsync(cancellationToken);
            while (await rows.ReadAsync(cancellationToken))
                traits.Add((rows.GetString(0), rows.GetString(1), new TraitValue((TraitCategory)rows.GetInt32(2), rows.GetString(3), "", 0,
                    Power: rows.IsDBNull(4) ? null : JsonSerializer.Deserialize<PowerInfo>(rows.GetString(4)))));
        }
        var characters = new List<(string Id, string? Predator, string? Advancement, string Clan, string ClanKey)>();
        await using (var read = connection.CreateCommand())
        {
            read.CommandText = "SELECT \"Id\", \"Details_PredatorSelection\", \"Details_Advancement\", \"Clan\", \"ClanKey\" FROM \"Characters\"";
            await using var rows = await read.ExecuteReaderAsync(cancellationToken);
            while (await rows.ReadAsync(cancellationToken))
                characters.Add((rows.GetString(0), rows.IsDBNull(1) ? null : rows.GetString(1), rows.IsDBNull(2) ? null : rows.GetString(2),
                    rows.GetString(3), rows.GetString(4)));
        }
        foreach (var (id, predator, advancement, clan, clanKey) in characters)
        {
            var own = traits.Where(x => x.CharacterId == id).ToArray();
            // Same (default) serializer options as the column conversions in VcsDbContext.
            var details = new CharacterDetails
            {
                Clan = clan, ClanKey = clanKey,
                PredatorSelection = predator is null ? null : JsonSerializer.Deserialize<PredatorSelection>(predator),
                Advancement = advancement is null ? null : JsonSerializer.Deserialize<AdvancementState>(advancement)
            };
            var (newDetails, newTraits) = converter.Upgrade(details, [.. own.Select(x => x.Value)]);
            foreach (var (trait, upgraded) in own.Zip(newTraits).Where(x => x.First.Value.Key != x.Second.Key))
                await db.Database.ExecuteSqlAsync($"UPDATE \"CharacterTraits\" SET \"Key\" = {upgraded.Key} WHERE \"Id\" = {trait.Id}", cancellationToken);
            if (newDetails != details)
                await db.Database.ExecuteSqlAsync(
                    $"UPDATE \"Characters\" SET \"Details_PredatorSelection\" = {Json(newDetails.PredatorSelection)}, \"Details_Advancement\" = {Json(newDetails.Advancement)}, \"ClanKey\" = {newDetails.ClanKey} WHERE \"Id\" = {id}",
                    cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    private static string? Json<T>(T? value) where T : class => value is null ? null : JsonSerializer.Serialize(value);

    // Frozen copy of the hash the old ids used, so they keep converting even if catalog text handling changes.
    private static string OldStableId(string prefix, string value) => prefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        string.Concat(value.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
            .Normalize(NormalizationForm.FormC).Trim().ToUpperInvariant())));

    [GeneratedRegex("^[0-9a-f]{32}$")]
    private static partial Regex Guid32();

    [GeneratedRegex("^(?:disc|power)-([0-9A-F]{64})$")]
    private static partial Regex OldHash();

    [GeneratedRegex(@"^lore-(.+)-([1-5])$")]
    private static partial Regex LoresheetId();

    [GeneratedRegex(@"^predator:(.+):([0-9]+)$")]
    private static partial Regex PredatorGrant();
}
