using System.Globalization;

namespace VCS.Domain.Characters;

public sealed record ClanDefinition(
    string Name, string Description, IReadOnlyList<string> Disciplines, IReadOnlyList<string> Aliases);

public static class ClanCatalog
{
    public static IReadOnlyList<ClanDefinition> All { get; } = Array.AsReadOnly(new[]
    {
        Clan("Banu Haqim", "Juízes, estudiosos e assassinos com forte ligação ao controle de sangue.",
            ["Feitiçaria de Sangue", "Celeridade", "Ofuscação"], ["Assamitas", "Assamita"]),
        Clan("Brujah", "Filósofos, rebeldes e ativistas apaixonados, espinha dorsal do movimento Anarquista.",
            ["Celeridade", "Potência", "Presença"]),
        Clan("Gangrel", "Nômades selvagens e predadores que vivem em sintonia com a Besta e a natureza.",
            ["Animalismo", "Fortitude", "Proteanismo"]),
        Clan("Malkavianos", "Videntes e lunáticos marcados pela loucura e por percepções além da realidade.",
            ["Auspícios", "Dominação", "Ofuscação"], ["Malkaviano", "Malkavian"]),
        Clan("O Ministério", "Manipuladores de fé, vícios e desejos proibidos.",
            ["Ofuscação", "Presença", "Proteanismo"], ["Ministério", "Setitas", "Setita"]),
        Clan("Nosferatu", "Espiões e informantes amaldiçoados com uma aparência monstruosa que os força a viver nas sombras e esgotos.",
            ["Animalismo", "Ofuscação", "Potência"]),
        Clan("Ravnos", "Ilusionistas e andarilhos de espírito livre.",
            ["Animalismo", "Ofuscação", "Presença"]),
        Clan("Salubri", "Curadores e guerreiros raros, historicamente perseguidos, com um terceiro olho místico.",
            ["Auspícios", "Dominação", "Fortitude"]),
        Clan("Toreador", "Artistas e sedutores obcecados pela beleza, estética e sensações humanas.",
            ["Auspícios", "Celeridade", "Presença"]),
        Clan("Tremere", "Feiticeiros do sangue que operam com rituais arcanos e hierarquias rígidas.",
            ["Auspícios", "Dominação", "Feitiçaria de Sangue"]),
        Clan("Tzimisce", "Moldadores de carne e osso (Vicissitude), historicamente associados ao Sabá e ao conde Drácula.",
            ["Animalismo", "Dominação", "Proteanismo"]),
        Clan("Ventrue", "Os aristocratas, líderes e \"reis\" do mundo vampírico, focados em controle e poder.",
            ["Dominação", "Fortitude", "Presença"]),
        Clan("Hecata", "Famílias de necromantes obcecadas pela morte e pelos espíritos.",
            ["Auspícios", "Fortitude", "Oblívio"], ["Giovanni"]),
        Clan("Lasombra", "Mestres das sombras e da manipulação, fundadores do Sabá que recentemente buscaram espaço na Camarilla.",
            ["Dominação", "Oblívio", "Potência"])
    });

    private static ClanDefinition Clan(string name, string description, string[] disciplines, string[]? aliases = null) =>
        new(name, description, Array.AsReadOnly(disciplines), Array.AsReadOnly(aliases ?? []));

    public static bool SameName(string first, string second) => CultureInfo.InvariantCulture.CompareInfo.Compare(
        first.Trim(), second.Trim(), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0;

    public static ClanDefinition? Find(string name) =>
        All.FirstOrDefault(x => SameName(x.Name, name) || x.Aliases.Any(alias => SameName(alias, name)));
}

public static class DisciplineAdvancement
{
    public static bool? IsInClan(string clan, string discipline)
    {
        var definition = ClanCatalog.Find(clan);
        return definition is null ? null : definition.Disciplines.Any(x => ClanCatalog.SameName(x, discipline));
    }

    public static int? NextLevelCost(string clan, string discipline, int currentLevel)
    {
        if (currentLevel is < 0 or > 5)
            throw new ArgumentOutOfRangeException(nameof(currentLevel), "O nivel deve estar entre 0 e 5.");
        if (currentLevel == 5) return null;
        var inClan = IsInClan(clan, discipline);
        return inClan is null ? null : (currentLevel + 1) * (inClan.Value ? 5 : 7);
    }
}
