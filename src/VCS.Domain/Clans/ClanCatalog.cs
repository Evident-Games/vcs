using System.Globalization;

namespace VCS.Domain.Clans;

public sealed record ClanDefinition(
    string Name, string Description, IReadOnlyList<string> Disciplines, IReadOnlyList<string> Aliases,
    string Bane = "");

public static class ClanCatalog
{
    public static IReadOnlyList<ClanDefinition> All { get; } = Array.AsReadOnly(new[]
    {
        Clan("Banu Haqim", "Juízes, estudiosos e assassinos com forte ligação ao controle de sangue.",
            ["Feitiçaria de Sangue", "Celeridade", "Ofuscação"], ["Assamitas", "Assamita"],
            bane: "Sede de Sangue Justo — sentem uma compulsão irresistível por beber sangue de outros vampiros, especialmente dos que cometeram transgressões."),
        Clan("Brujah", "Filósofos, rebeldes e ativistas apaixonados, espinha dorsal do movimento Anarquista.",
            ["Celeridade", "Potência", "Presença"],
            bane: "Sangue Fervoroso — têm temperamento explosivo e podem cair mais facilmente em frenesi de fúria quando provocados ou sob estresse."),
        Clan("Gangrel", "Nômades selvagens e predadores que vivem em sintonia com a Besta e a natureza.",
            ["Animalismo", "Fortitude", "Proteanismo"],
            bane: "Características Bestiais — ao entrar em frenesi, ganham traços animais temporários ou permanentes, como orelhas, pelo, olhos ou garras."),
        Clan("Malkavianos", "Videntes e lunáticos marcados pela loucura e por percepções além da realidade.",
            ["Auspícios", "Dominação", "Ofuscação"], ["Malkaviano", "Malkavian"],
            bane: "Perspectiva Fraturada — carregam um transtorno mental permanente ou delírios profundos, ligados à percepção sobrenatural do clã."),
        Clan("O Ministério", "Manipuladores de fé, vícios e desejos proibidos.",
            ["Ofuscação", "Presença", "Proteanismo"], ["Ministério", "Setitas", "Setita"],
            bane: "Aversão à Luz — são extremamente sensíveis à luz intensa e ao sol."),
        Clan("Nosferatu", "Espiões e informantes amaldiçoados com uma aparência monstruosa que os força a viver nas sombras e esgotos.",
            ["Animalismo", "Ofuscação", "Potência"],
            bane: "Aparência Repulsiva — o Abraço deforma o corpo e dificulta disfarçar-se ou passar por mortal."),
        Clan("Ravnos", "Ilusionistas e andarilhos de espírito livre.",
            ["Animalismo", "Ofuscação", "Presença"],
            bane: "Incapacidade de Raiz — não conseguem permanecer muito tempo no mesmo lugar sem atrair azar e perseguição."),
        Clan("Salubri", "Curadores e guerreiros raros, historicamente perseguidos, com um terceiro olho místico.",
            ["Auspícios", "Dominação", "Fortitude"],
            bane: "Terceiro Olho — carregam um olho vertical na testa que denuncia sua herança e atrai perseguição."),
        Clan("Toreador", "Artistas e sedutores obcecados pela beleza, estética e sensações humanas.",
            ["Auspícios", "Celeridade", "Presença"],
            bane: "Fixação / Obsessão Estética — sofrem redutores em paradas de Disciplinas quando estão em ambientes que consideram feios ou monótonos."),
        Clan("Tremere", "Feiticeiros do sangue que operam com rituais arcanos e hierarquias rígidas.",
            ["Auspícios", "Dominação", "Feitiçaria de Sangue"],
            bane: "Sangue Imperfeito — têm dificuldade para criar laços de sangue e novos carniçais; a história do clã também é marcada por hierarquia e paranoia."),
        Clan("Tzimisce", "Moldadores de carne e osso (Vicissitude), historicamente associados ao Sabá e ao conde Drácula.",
            ["Animalismo", "Dominação", "Proteanismo"],
            bane: "Enraizados (Posse) — precisam escolher uma posse, local ou grupo ao qual se ligam e sentem ansiedade ou fraqueza se dormirem longe dela."),
        Clan("Ventrue", "Os aristocratas, líderes e \"reis\" do mundo vampírico, focados em controle e poder.",
            ["Dominação", "Fortitude", "Presença"],
            bane: "Paladar Refinado — só conseguem alimentar-se de um tipo específico de mortal; outro sangue é rejeitado, salvo esforço de Força de Vontade."),
        Clan("Hecata", "Famílias de necromantes obcecadas pela morte e pelos espíritos.",
            ["Auspícios", "Fortitude", "Oblívio"], ["Giovanni"],
            bane: "Banquete Doloroso — a mordida de alimentação causa dor lancinante ao mortal, dificultando caçadas discretas."),
        Clan("Lasombra", "Mestres das sombras e da manipulação, fundadores do Sabá que recentemente buscaram espaço na Camarilla.",
            ["Dominação", "Oblívio", "Potência"],
            bane: "Ausência de Reflexo — não aparecem em espelhos, superfícies polidas, câmeras digitais ou fotografias, dificultando manter a Máscara.")
    });

    private static ClanDefinition Clan(string name, string description, string[] disciplines,
        string[]? aliases = null, string bane = "") =>
        new(name, description, Array.AsReadOnly(disciplines), Array.AsReadOnly(aliases ?? []), bane);

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
