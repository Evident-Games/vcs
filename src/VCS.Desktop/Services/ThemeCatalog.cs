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
        new ThemeDefinition("yellow-red", "Amarelo e vermelho", "YellowRed", false)
    });

    public static ThemeDefinition Get(string id) => All.FirstOrDefault(x => x.Id == id)
        ?? throw new ArgumentException("Tema desconhecido: " + id, nameof(id));
}
