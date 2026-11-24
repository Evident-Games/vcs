using Game.Rules.V5.Dice;
using Game.Rules.V5.Disciplines;
using Game.Rules.V5.Rituals;
using Game.Sheet;

namespace Game.Application.Blood;

/// <summary>Resolve a confirmed sanctuary travel attempt without moving or changing any character.</summary>
public sealed class TrueSanctuaryActions
{
    public SanctuaryCirclePairRegistration RegisterCirclePair(IReadOnlyList<PowerEffectEntry> entries,
        string departure, string destination, bool departureFixedSurfaceConfirmed,
        bool destinationFixedSurfaceConfirmed, bool noEndpointInVehicleConfirmed,
        bool tableConfirmed, bool exceptionApproved)
    {
        if (!tableConfirmed)
            throw new InvalidOperationException("Confirme com a mesa os locais designados para partida e chegada.");
        var validation = TrueSanctuaryRules.ValidateCirclePair(departure, destination,
            departureFixedSurfaceConfirmed, destinationFixedSurfaceConfirmed,
            noEndpointInVehicleConfirmed, exceptionApproved);
        var pair = validation.Pair;
        var current = entries.LastOrDefault(x => x.Active && x.IsSelf &&
            x.PowerId == "ritual-sanctuary-circle-pair" && x.CirclePair is not null);
        var preparation = entries.Where(x => x.Active && x.IsSelf &&
            x.PowerId == "ritual-sanctuary-preparation-check" && x.PreparationStep is not null).ToArray();
        var historyMismatch = preparation.Any(x => !string.IsNullOrWhiteSpace(x.PreparationStep!.Location) &&
            !string.Equals(x.PreparationStep.Location,
                x.PreparationStep.Circle == 1 ? pair.Departure : pair.Destination,
                StringComparison.OrdinalIgnoreCase));
        var correction = historyMismatch || current?.CirclePair is { } existing &&
            !TrueSanctuaryRules.CirclePairMatches(existing, pair);
        if (correction && !exceptionApproved)
            throw new InvalidOperationException("A correção de um par já registrado exige autorização e justificativa do Narrador.");
        var updated = preparation.Select(entry =>
        {
            var location = entry.PreparationStep!.Circle == 1 ? pair.Departure : pair.Destination;
            return entry with
            {
                Target = $"Círculo {entry.PreparationStep.Circle}: {location}",
                PreparationStep = entry.PreparationStep with { Location = location }
            };
        }).ToArray();
        return new(pair, current is null ? null : current with { Active = false }, updated, correction,
            validation.PlacementExceptionUsed);
    }

    public SanctuaryPairInvalidation InvalidatePairAfterCircleDamage(
        IReadOnlyList<PowerEffectEntry> preparationEntries, int damagedCircle, bool tableConfirmed)
    {
        var summary = TrueSanctuaryRules.CircleDamageEnding(damagedCircle, tableConfirmed);
        var active = preparationEntries.Where(x => x.Active && x.IsSelf &&
            ((x.PowerId == "ritual-sanctuary-preparation-check" && x.PreparationStep is not null) ||
             (x.PowerId == "ritual-sanctuary-circle-pair" && x.CirclePair is not null))).ToArray();
        if (active.All(x => x.PreparationStep is null))
            throw new InvalidOperationException("Não há preparação ativa do Santuário para inutilizar.");
        var ended = active.Select(x => x with
        {
            Active = false,
            Effect = $"{x.Effect} {summary}",
            Duration = "Par de círculos inerte após dano confirmado"
        }).ToArray();
        return new(ended, summary);
    }

    public SanctuaryPreparationCheckResult PrepareNextCheck(int circle, DateOnly nightDate,
        IReadOnlyList<RitualPreparationStep> paidChecks, RitualCirclePair circlePair, int hunger, bool conditionsConfirmed,
        bool scheduleExceptionApproved = false, Random? random = null)
    {
        if (!conditionsConfirmed)
            throw new InvalidOperationException("Confirme com o Narrador o círculo, os ingredientes e o tempo de preparação desta noite.");
        var advance = TrueSanctuaryRules.NextPreparationCheck(circle, nightDate, paidChecks, circlePair, scheduleExceptionApproved);
        if (hunger is < 0 or >= 5)
            throw new InvalidOperationException("Fome 5 impede despertar o Sangue voluntariamente; nenhum custo foi rolado.");
        return new(advance, RouseCheckRules.Roll(hunger, random));
    }

    public SanctuaryTravelResult AttemptTravel(int pool, int hunger, bool circlesComplete, bool circlesIntact,
        bool atDepartureCircle, bool concentratedForTurn, bool alreadyAttemptedThisScene,
        bool carriesPerson, bool carriesObjects, string payloadDescription, bool objectMassConfirmed,
        RitualCirclePair? circlePair, bool persistedPairRequired, bool pairExceptionApproved,
        Random? random = null, bool beastSubdued = false, bool sceneLimitExceptionApproved = false)
    {
        var pairExceptionUsed = (persistedPairRequired && circlePair is null) ||
            (circlePair is not null && !TrueSanctuaryRules.HasValidOrApprovedPlacement(circlePair));
        if (pairExceptionUsed && !pairExceptionApproved)
            throw new InvalidOperationException(
                "A preparação registrada precisa identificar os dois círculos em superfícies fixas fora de veículos.");
        if (!TrueSanctuaryRules.CanAttemptTravel(circlesComplete, circlesIntact, atDepartureCircle,
            concentratedForTurn, alreadyAttemptedThisScene && !sceneLimitExceptionApproved))
            throw new InvalidOperationException("Confirme os círculos completos e intactos, a posição, a concentração e o limite de uma tentativa por cena.");
        var payload = TrueSanctuaryRules.ValidatePayload(carriesPerson, carriesObjects, payloadDescription,
            objectMassConfirmed);
        if (pool is < 0 or > 50) throw new ArgumentOutOfRangeException(nameof(pool));
        if (hunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(hunger));
        var difficulty = RitualRules.DefaultRequirements(5).Difficulty;
        var originalRoll = DicePoolRules.Roll(pool, hunger, difficulty, random);
        var roll = QuellBeastRules.ApplyToRoll(originalRoll, beastSubdued);
        return new(roll, payload, beastSubdued,
            originalRoll.Outcome == TestOutcome.MessyCritical && roll.Outcome == TestOutcome.CriticalSuccess,
            pairExceptionUsed);
    }
}

public sealed record SanctuaryPreparationCheckResult(SanctuaryPreparationAdvance Advance, RouseCheckResult Check);

public sealed record SanctuaryCirclePairRegistration(RitualCirclePair Pair,
    PowerEffectEntry? EndedPair, IReadOnlyList<PowerEffectEntry> UpdatedPreparation, bool CorrectionUsed,
    bool PlacementExceptionUsed);

public sealed record SanctuaryPairInvalidation(IReadOnlyList<PowerEffectEntry> EndedEntries, string Summary);

public sealed record SanctuaryTravelResult(DicePoolResult Roll, SanctuaryTravelPayload Payload,
    bool BeastSubduedConsidered,
    bool MessyCriticalConverted,
    bool PairExceptionUsed);
