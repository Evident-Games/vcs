using Game.Rules.Modules;
using Game.Rules.V5.Modules;

namespace Game.Tests;

public sealed class V5ModuleTests
{
    [Fact]
    public void Installed_v5_module_resolves_for_existing_sheets()
    {
        var registry = new RuleModuleRegistry([V5Module.Manifest]);
        var selection = registry.Resolve("V5");
        Assert.Equal("V5", selection.EditionId);
        Assert.Equal("v5-core", Assert.Single(selection.Modules).ModuleId);
    }

    [Theory]
    [InlineData("V3")]
    [InlineData("V20")]
    public void Uninstalled_editions_are_not_implicitly_supported(string edition)
    {
        var registry = new RuleModuleRegistry([V5Module.Manifest]);
        Assert.Throws<NotSupportedException>(() => registry.Resolve(edition));
    }
}
