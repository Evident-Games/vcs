namespace VCS.Desktop.Services;

public sealed record ThemeDefinition(string Id, string Name, string ResourceName, bool IsDark);

public static class ThemeCatalog
{
    public const string DefaultId = "white-red";
    public static IReadOnlyList<ThemeDefinition> All { get; } = Array.AsReadOnly(new[]
    {
        new ThemeDefinition("black-white", "Preto e branco", "BlackWhite", true),
        new ThemeDefinition("white-black", "Branco e preto", "WhiteBlack", false),
        new ThemeDefinition(DefaultId, "Branco e vermelho", "Light", false),
        new ThemeDefinition("black-red", "Preto e vermelho", "Dark", true),
        new ThemeDefinition("yellow-red", "Amarelo e vermelho", "YellowRed", false),
        new ThemeDefinition("banu-haqim", "Banu Haqim", "BanuHaqim", false),
        new ThemeDefinition("brujah", "Brujah", "Brujah", false),
        new ThemeDefinition("gangrel", "Gangrel", "Gangrel", false),
        new ThemeDefinition("malkavian", "Malkavian", "Malkavian", false),
        new ThemeDefinition("nosferatu", "Nosferatu", "Nosferatu", false),
        new ThemeDefinition("tremere", "Tremere", "Tremere", false),
        new ThemeDefinition("ventrue", "Ventrue", "Ventrue", false),
        new ThemeDefinition("toreador", "Toreador", "Toreador", false),
        new ThemeDefinition("caitiff", "Caitiff", "Caitiff", false),
        new ThemeDefinition("ministerio", "Ministerio", "Ministerio", false),
        new ThemeDefinition("ravnos", "Ravnos", "Ravnos", false),
        new ThemeDefinition("salubri", "Salubri", "Salubri", false),
        new ThemeDefinition("tzimisce", "Tzimisce", "Tzimisce", false),
        new ThemeDefinition("sangue-ralo", "Sangue-Ralo", "SangueRalo", false),
    });

    public static ThemeDefinition Get(string id) => All.FirstOrDefault(x => x.Id == id)
        ?? throw new ArgumentException("Tema desconhecido: " + id, nameof(id));
}
