namespace VCS.Desktop.Services;

public enum ThemeGroup { Default, Clan, Special }

public sealed record ThemeDefinition(string Id, string Name, ThemeGroup Group, string ResourceName)
{
    /// <summary>Theme palettes live in Themes/Default or Themes/Clan, named after <see cref="ResourceName"/>.</summary>
    public string ResourcePath => $"/VCS.Desktop;component/Themes/{Group}/{ResourceName}.xaml";
}

public static class ThemeCatalog
{
    public const string DefaultId = "white-red";
    public const string SpecialId = "coterie";
    public static IReadOnlyList<ThemeDefinition> All { get; } = Array.AsReadOnly(new[]
    {
        new ThemeDefinition("black-white", "Preto e branco", ThemeGroup.Default, "BlackWhite"),
        new ThemeDefinition("white-black", "Branco e preto", ThemeGroup.Default, "WhiteBlack"),
        new ThemeDefinition(DefaultId, "Branco e vermelho", ThemeGroup.Default, "Light"),
        new ThemeDefinition("black-red", "Preto e vermelho", ThemeGroup.Default, "Dark"),
        new ThemeDefinition("yellow-red", "Amarelo e vermelho", ThemeGroup.Default, "YellowRed"),
        new ThemeDefinition("banu-haqim", "Banu Haqim", ThemeGroup.Clan, "BanuHaqim"),
        new ThemeDefinition("brujah", "Brujah", ThemeGroup.Clan, "Brujah"),
        new ThemeDefinition("caitiff", "Caitiff", ThemeGroup.Clan, "Caitiff"),
        new ThemeDefinition("gangrel", "Gangrel", ThemeGroup.Clan, "Grangel"),
        new ThemeDefinition("hecata", "Hecata", ThemeGroup.Clan, "Hecata"),
        new ThemeDefinition("lasombra", "Lasombra", ThemeGroup.Clan, "Lasombra"),
        new ThemeDefinition("malkavian", "Malkavianos", ThemeGroup.Clan, "Malkavianos"),
        new ThemeDefinition("nosferatu", "Nosferatu", ThemeGroup.Clan, "Nosferatu"),
        new ThemeDefinition("ministerio", "O Ministério", ThemeGroup.Clan, "Oministerio"),
        new ThemeDefinition("ravnos", "Ravnos", ThemeGroup.Clan, "Ravnos"),
        new ThemeDefinition("salubri", "Salubri", ThemeGroup.Clan, "Salubri"),
        new ThemeDefinition("sangue-ralo", "Sangue-Ralo", ThemeGroup.Clan, "SangueRalos"),
        new ThemeDefinition("toreador", "Toreador", ThemeGroup.Clan, "Toreador"),
        new ThemeDefinition("tremere", "Tremere", ThemeGroup.Clan, "Tremere"),
        new ThemeDefinition("tzimisce", "Tzimisce", ThemeGroup.Clan, "Tzimisce"),
        new ThemeDefinition("ventrue", "Ventrue", ThemeGroup.Clan, "Ventrue"),
        new ThemeDefinition(SpecialId, "O Sínido de Alabastro", ThemeGroup.Special, "Coterie"),

    });

    public static ThemeDefinition Get(string id) => All.FirstOrDefault(x => x.Id == id)
        ?? throw new ArgumentException("Tema desconhecido: " + id, nameof(id));
}