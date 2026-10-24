using VCS.Domain.Clans;

namespace VCS.Desktop.Sheet;

public static class DisciplineXpText
{
    public static string Describe(string clan, string discipline, int rating)
    {
        var inClan = DisciplineAdvancement.IsInClan(clan, discipline);
        if (inClan is null) return "Cla nao catalogado: custo de XP nao definido.";
        var kind = inClan.Value ? "Do cla" : "Fora do cla";
        if (rating == 5) return $"{kind} · Nivel maximo (5).";
        var cost = DisciplineAdvancement.NextLevelCost(clan, discipline, rating);
        return $"{kind} · Proximo nivel ({rating + 1}): {cost} XP · novo nivel x {(inClan.Value ? 5 : 7)}.";
    }
}
