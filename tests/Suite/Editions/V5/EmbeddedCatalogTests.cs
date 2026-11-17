using System.Text.Json;
using VCS.Content.V5;

namespace VCS.Tests;

public sealed class EmbeddedCatalogTests
{
    [Theory]
    [InlineData("advantages.json")]
    [InlineData("clans.json")]
    [InlineData("disciplines.json")]
    [InlineData("loresheets.json")]
    [InlineData("power-uses.json")]
    [InlineData("powers.json")]
    [InlineData("predators.json")]
    [InlineData("ritual-uses.json")]
    [InlineData("skills.json")]
    public void V5_catalog_is_embedded_under_its_existing_name(string file)
    {
        var assembly = typeof(V5ContentResources).Assembly;
        var name = "VCS.Content.Data." + file;
        Assert.Contains(name, assembly.GetManifestResourceNames());
        using var stream = assembly.GetManifestResourceStream(name);
        Assert.NotNull(stream);
        using var document = JsonDocument.Parse(stream);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
    }
}
