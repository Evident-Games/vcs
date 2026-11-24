using Game.Content;
using Game.Sheet;

namespace Game.Rules.V5.Rituals;

/// <summary>Preparation requirements for Escape to the True Sanctuary.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), p. 281")]
public static class TrueSanctuaryRules
{
    public static TrueSanctuaryPreparationRequirements Preparation() => new(2, 3, 120, 2);

    public static SanctuaryCirclePairValidation ValidateCirclePair(string departure, string destination,
        bool departureFixedSurfaceConfirmed, bool destinationFixedSurfaceConfirmed,
        bool noEndpointInVehicleConfirmed, bool circleDiameterConfirmed,
        bool pairExceptionApproved = false)
    {
        var departureLocation = departure.Trim();
        var destinationLocation = destination.Trim();
        if (departureLocation.Length == 0 || destinationLocation.Length == 0)
            throw new InvalidOperationException("Identifique os locais dos círculos de partida e de chegada.");
        if (string.Equals(departureLocation, destinationLocation, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Os círculos de partida e de chegada precisam ter identificações distintas.");
        var placementValid = departureFixedSurfaceConfirmed && destinationFixedSurfaceConfirmed &&
            noEndpointInVehicleConfirmed;
        if (!placementValid && !pairExceptionApproved)
            throw new InvalidOperationException(
                "Confirme que os dois círculos estão no solo ou piso de uma construção e que nenhum está em veículo.");
        if (!circleDiameterConfirmed && !pairExceptionApproved)
            throw new InvalidOperationException(
                "Confirme que cada círculo possui aproximadamente um metro de diâmetro.");
        return new(new(departureLocation, destinationLocation, departureFixedSurfaceConfirmed,
            destinationFixedSurfaceConfirmed, noEndpointInVehicleConfirmed,
            !placementValid && pairExceptionApproved, circleDiameterConfirmed,
            !circleDiameterConfirmed && pairExceptionApproved), !placementValid, !circleDiameterConfirmed);
    }

    public static bool HasValidOrApprovedPlacement(RitualCirclePair pair) =>
        (pair.DepartureFixedSurfaceConfirmed && pair.DestinationFixedSurfaceConfirmed &&
         pair.NoEndpointInVehicleConfirmed) || pair.PlacementExceptionApproved;

    public static bool HasValidOrApprovedDiameter(RitualCirclePair pair) =>
        pair.CircleDiameterConfirmed || pair.DiameterExceptionApproved;

    public static bool HasValidOrApprovedPair(RitualCirclePair pair) =>
        HasValidOrApprovedPlacement(pair) && HasValidOrApprovedDiameter(pair);

    public static bool CirclePairMatches(RitualCirclePair current, RitualCirclePair proposed) =>
        string.Equals(current.Departure, proposed.Departure, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(current.Destination, proposed.Destination, StringComparison.OrdinalIgnoreCase);

    public static string CircleDamageEnding(int damagedCircle, bool tableConfirmed)
    {
        if (damagedCircle is < 1 or > 2) throw new ArgumentOutOfRangeException(nameof(damagedCircle));
        if (!tableConfirmed)
            throw new InvalidOperationException("Confirme com a mesa qual círculo sofreu dano antes de inutilizar o par.");
        return $"Dano confirmado no círculo {damagedCircle}: o ritual foi arruinado e os dois círculos ficaram inertes.";
    }

    public static SanctuaryTravelPayload ValidatePayload(bool carriesPerson, bool carriesObjects,
        string description, bool objectMassConfirmed)
    {
        if (carriesPerson && carriesObjects)
            throw new InvalidOperationException("A viagem permite uma pessoa ou uma carga de objetos, não as duas opções juntas.");
        if (!carriesPerson && !carriesObjects)
        {
            if (!string.IsNullOrWhiteSpace(description))
                throw new InvalidOperationException("Marque se a descrição informada representa uma pessoa ou uma carga de objetos.");
            return new(false, false, "", "Sem passageiro ou carga adicional.");
        }
        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException("Identifique a pessoa ou a carga proposta para a viagem.");
        if (carriesObjects && !objectMassConfirmed)
            throw new InvalidOperationException("Confirme com a mesa que a massa total dos objetos é aproximadamente a de uma pessoa.");
        var target = description.Trim();
        return new(carriesPerson, carriesObjects, target,
            carriesPerson ? $"Passageiro proposto: {target}." : $"Carga de objetos proposta dentro do limite confirmado: {target}.");
    }

    public static bool IsPreparationComplete(IReadOnlyList<RitualPreparationStep> paidChecks)
    {
        var requirements = Preparation();
        var count = requirements.ConsecutiveNightsPerCircle * requirements.RouseChecksPerCirclePerNight;
        return Enumerable.Range(1, requirements.CircleCount).All(circle =>
            paidChecks.Where(x => x.Circle == circle).Select(x => x.Sequence).Order().SequenceEqual(Enumerable.Range(1, count)));
    }

    public static SanctuaryPreparationAdvance NextPreparationCheck(int circle, DateOnly nightDate,
        IReadOnlyList<RitualPreparationStep> paidChecks, RitualCirclePair circlePair,
        bool scheduleExceptionApproved = false)
    {
        var requirements = Preparation();
        if (circle < 1 || circle > requirements.CircleCount) throw new ArgumentOutOfRangeException(nameof(circle));
        if (!HasValidOrApprovedPair(circlePair))
            throw new InvalidOperationException(
                "Reconfirme o diâmetro aproximado de um metro e que os dois círculos estão em superfícies fixas fora de veículos antes de pagar outra checagem.");
        var previous = paidChecks.Where(x => x.Circle == circle).OrderBy(x => x.Sequence).ToArray();
        for (var index = 0; index < previous.Length; index++)
            if (previous[index].Sequence != index + 1)
                throw new InvalidOperationException("Histórico de preparação inconsistente; confira os registros com o Narrador antes de pagar outra checagem.");
        var location = circle == 1 ? circlePair.Departure : circlePair.Destination;
        if (previous.Any(x => !string.IsNullOrWhiteSpace(x.Location) &&
            !string.Equals(x.Location, location, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("O local deste círculo não corresponde à preparação já registrada. Corrija o par com confirmação e justificativa do Narrador.");
        var checksPerCircle = requirements.ConsecutiveNightsPerCircle * requirements.RouseChecksPerCirclePerNight;
        if (previous.Length >= checksPerCircle)
            throw new InvalidOperationException("Este círculo já possui todas as checagens de preparação registradas.");
        var expectedDate = previous.Length == 0 ? nightDate :
            previous[^1].NightDate.AddDays(previous.Length % requirements.RouseChecksPerCirclePerNight == 0 ? 1 : 0);
        var exceptionUsed = expectedDate != nightDate;
        if (exceptionUsed && !scheduleExceptionApproved)
            throw new InvalidOperationException($"A próxima checagem deste círculo deve ocorrer em {expectedDate:dd/MM/yyyy}. Confira a data ou justifique a decisão do Narrador sobre a interrupção.");
        return new(new(circle, previous.Length + 1, nightDate, location), exceptionUsed);
    }

    /// <summary>The table confirms the circles, their condition, concentration and attempts in the scene.</summary>
    public static string ValidateTravelConditions(bool circlesComplete, bool circlesIntact,
        bool atDepartureCircle, bool concentratedForTurn, bool alreadyAttemptedThisScene)
    {
        if (!circlesComplete)
            throw new InvalidOperationException("Confirme que os dois círculos estão completos antes da tentativa.");
        if (!circlesIntact)
            throw new InvalidOperationException("Confirme que os dois círculos permanecem intactos antes da tentativa.");
        if (!atDepartureCircle)
            throw new InvalidOperationException("Confirme que o conjurador está no círculo designado como partida.");
        if (!concentratedForTurn)
            throw new InvalidOperationException("Confirme que o conjurador se concentrou por um turno antes da rolagem.");
        if (alreadyAttemptedThisScene)
            throw new InvalidOperationException("O ritual permite somente uma tentativa de viagem por cena.");
        return "Círculos completos e intactos; conjurador no círculo de partida; concentração de um turno confirmada.";
    }
}

public sealed record SanctuaryPreparationAdvance(RitualPreparationStep Step, bool ScheduleExceptionUsed);

public sealed record SanctuaryCirclePairValidation(RitualCirclePair Pair, bool PlacementExceptionUsed,
    bool DiameterExceptionUsed);

/// <summary>A travel payload is only a recorded proposal; another sheet or inventory is never changed.</summary>
public sealed record SanctuaryTravelPayload(bool CarriesPerson, bool CarriesObjects, string Description,
    string Summary);

/// <summary>Each circle is consecrated over consecutive nights; the table tracks the schedule.</summary>
public sealed record TrueSanctuaryPreparationRequirements(
    int CircleCount, int ConsecutiveNightsPerCircle, int MinutesPerCirclePerNight, int RouseChecksPerCirclePerNight)
{
    public int TotalRouseChecks => CircleCount * ConsecutiveNightsPerCircle * RouseChecksPerCirclePerNight;
}
