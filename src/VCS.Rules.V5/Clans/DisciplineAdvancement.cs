using VCS.Content.Clans;

namespace VCS.Rules.V5.Clans;

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
