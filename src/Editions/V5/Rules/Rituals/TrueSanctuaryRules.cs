using Game.Content;
using Game.Sheet;

namespace Game.Rules.V5.Rituals;

/// <summary>Preparation requirements for Escape to the True Sanctuary.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), p. 281")]
public static class TrueSanctuaryRules
{
    public static TrueSanctuaryPreparationRequirements Preparation() => new(2, 3, 120, 2);

    public static bool IsPreparationComplete(IReadOnlyList<RitualPreparationStep> paidChecks)
    {
        var requirements = Preparation();
        var count = requirements.ConsecutiveNightsPerCircle * requirements.RouseChecksPerCirclePerNight;
        return Enumerable.Range(1, requirements.CircleCount).All(circle =>
            paidChecks.Where(x => x.Circle == circle).Select(x => x.Sequence).Order().SequenceEqual(Enumerable.Range(1, count)));
    }

    public static SanctuaryPreparationAdvance NextPreparationCheck(int circle, DateOnly nightDate,
        IReadOnlyList<RitualPreparationStep> paidChecks, bool scheduleExceptionApproved = false)
    {
        var requirements = Preparation();
        if (circle < 1 || circle > requirements.CircleCount) throw new ArgumentOutOfRangeException(nameof(circle));
        var previous = paidChecks.Where(x => x.Circle == circle).OrderBy(x => x.Sequence).ToArray();
        for (var index = 0; index < previous.Length; index++)
            if (previous[index].Sequence != index + 1)
                throw new InvalidOperationException("Histórico de preparação inconsistente; confira os registros com o Narrador antes de pagar outra checagem.");
        var checksPerCircle = requirements.ConsecutiveNightsPerCircle * requirements.RouseChecksPerCirclePerNight;
        if (previous.Length >= checksPerCircle)
            throw new InvalidOperationException("Este círculo já possui todas as checagens de preparação registradas.");
        var expectedDate = previous.Length == 0 ? nightDate :
            previous[^1].NightDate.AddDays(previous.Length % requirements.RouseChecksPerCirclePerNight == 0 ? 1 : 0);
        var exceptionUsed = expectedDate != nightDate;
        if (exceptionUsed && !scheduleExceptionApproved)
            throw new InvalidOperationException($"A próxima checagem deste círculo deve ocorrer em {expectedDate:dd/MM/yyyy}. Confira a data ou justifique a decisão do Narrador sobre a interrupção.");
        return new(new(circle, previous.Length + 1, nightDate), exceptionUsed);
    }

    /// <summary>The table confirms the circles, their condition, concentration and attempts in the scene.</summary>
    public static bool CanAttemptTravel(bool circlesComplete, bool circlesIntact, bool atDepartureCircle,
        bool concentratedForTurn, bool alreadyAttemptedThisScene) =>
        circlesComplete && circlesIntact && atDepartureCircle && concentratedForTurn && !alreadyAttemptedThisScene;
}

public sealed record SanctuaryPreparationAdvance(RitualPreparationStep Step, bool ScheduleExceptionUsed);

/// <summary>Each circle is consecrated over consecutive nights; the table tracks the schedule.</summary>
public sealed record TrueSanctuaryPreparationRequirements(
    int CircleCount, int ConsecutiveNightsPerCircle, int MinutesPerCirclePerNight, int RouseChecksPerCirclePerNight)
{
    public int TotalRouseChecks => CircleCount * ConsecutiveNightsPerCircle * RouseChecksPerCirclePerNight;
}
