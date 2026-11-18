using Game.Application.Characters;

namespace Game.Desktop.Sheet;

public static class DisciplineXpText
{
    public static string Describe(DisciplineAdvancementGuidance guidance)
    {
        if (guidance.IsInClan is not { } inClan) return "Cla nao catalogado: custo de XP nao definido.";
        var kind = inClan ? "Do cla" : "Fora do cla";
        if (guidance.NextLevelCost is null) return $"{kind} · Nivel maximo (5).";
        return $"{kind} · Proximo nivel ({guidance.CurrentRating + 1}): {guidance.NextLevelCost} XP · novo nivel x {(inClan ? 5 : 7)}.";
    }
}
