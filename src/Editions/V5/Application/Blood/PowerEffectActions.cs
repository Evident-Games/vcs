using Game.Content.Disciplines;
using Game.Rules.V5.Disciplines;
using Game.Sheet;

namespace Game.Application.Blood;

public sealed record PowerEffectGuidance(string Description, int DamageBonus, bool IgnoresSuperficialReduction,
    bool CanUseObjects, bool CanSpeak, string Duration);

/// <summary>Core powers with a direct sheet calculation or an explicit application of their main numeric effect. Secondary and narrative rules can still require Storyteller adjudication.</summary>
public sealed class PowerEffectActions
{
    public void EnsureDominateTargetAvailable(IEnumerable<PowerEffectEntry> effects, string powerId, string target,
        bool exceptionApproved, string exceptionReason)
    {
        ArgumentNullException.ThrowIfNull(effects);
        if (!DominateOngoingRules.IsDominatePower(powerId) ||
            !effects.Any(x => x.DominateStoryLock && string.Equals(x.Target, target, StringComparison.OrdinalIgnoreCase)))
            return;
        if (!exceptionApproved)
            throw new InvalidOperationException("Uma falha total impede Dominação contra este alvo pelo restante da história.");
        if (string.IsNullOrWhiteSpace(exceptionReason))
            throw new InvalidOperationException("A exceção ao bloqueio de Dominação exige justificativa do Narrador.");
    }

    public PowerEffectEntry EndDominateStoryLock(PowerEffectEntry entry, bool storyEndedConfirmed)
    {
        if (!entry.DominateStoryLock)
            throw new InvalidOperationException("Selecione um registro de falha total de Dominação ainda bloqueado.");
        if (!storyEndedConfirmed)
            throw new InvalidOperationException("Confirme com a mesa que a história terminou.");
        return entry with
        {
            DominateStoryLock = false,
            Effect = $"{entry.Effect} Bloqueio encerrado após confirmação do fim da história."
        };
    }

    public PowerEffectEntry AdvanceConcealmentNight(PowerEffectEntry entry, DateOnly completedNight,
        bool tableConfirmed)
    {
        if (entry.PowerId != "power-obfuscate-conceal" || !entry.Active)
            throw new InvalidOperationException("Selecione um efeito ativo de Ocultar.");
        if (!tableConfirmed)
            throw new InvalidOperationException("Confirme com a mesa que a noite informada terminou na crônica.");
        if (entry.GameDate is not { } startedAt)
            throw new InvalidOperationException("O registro não possui data inicial; o saldo legado não pode ser avançado automaticamente.");
        var remaining = ObfuscateOngoingRules.AdvanceConcealmentNights(entry.NightsRemaining,
            DateOnly.FromDateTime(startedAt), entry.LastNightAdvanced, completedNight);
        return entry with
        {
            Active = remaining > 0,
            NightsRemaining = remaining,
            LastNightAdvanced = completedNight,
            Duration = remaining > 0 ? $"{remaining} noite(s) restante(s)" : "Encerrado após as noites confirmadas"
        };
    }

    public static IReadOnlySet<string> IntegratedMechanicalPowerIds { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "power-animalism-animal-succulence",
        "power-celerity-fleetness",
        "power-auspex-heightened-senses",
        "power-presence-awe",
        "power-fortitude-fortify-the-inner-facade",
        "power-fortitude-resilience",
        "power-fortitude-unswayable-mind",
        "power-fortitude-toughness",
        "power-fortitude-prowess-from-pain",
        "power-fortitude-flesh-of-marble",
        "power-potence-prowess",
        "power-protean-feral-weapons",
        "power-animalism-bond-famulus",
        "power-animalism-quell-the-beast",
        "power-celerity-cats-grace",
        "power-celerity-rapid-reflexes",
        "power-celerity-unerring-aim",
        "power-celerity-lightning-strike",
        "power-fortitude-defy-bane",
        "power-obfuscate-ghost-in-the-machine",
        "power-potence-lethal-body",
        "power-potence-uncanny-grip",
        "power-potence-fist-of-caine",
        "power-presence-daunt",
        "power-presence-entrancement",
        "power-protean-weight-of-the-feather",
        "power-protean-eyes-of-the-beast",
        "power-protean-shapechange",
        "power-protean-metamorphosis",
        "power-protean-unfettered-heart",
        "power-protean-mist-form",
        "power-blood-sorcery-blood-of-potency",
        "power-dominate-dementation",
        "power-blood-sorcery-extinguish-vitae",
        "power-blood-sorcery-scorpions-touch",
        "power-blood-sorcery-baals-caress",
        "power-blood-sorcery-cauldron-of-blood"
    };

    public PowerEffectGuidance Describe(PowerDefinition power) => power.Id switch
    {
        "power-protean-feral-weapons" => new(power.Summary, 2, true, true, true, "Uma cena"),
        "power-protean-shapechange" or "power-protean-metamorphosis" => new(power.Summary, 0, false, false, false, "Uma cena ou encerramento voluntário"),
        "power-protean-mist-form" => new(power.Summary, 0, false, false, false, "Uma cena ou encerramento voluntário"),
        _ => new(power.Summary, 0, false, true, true, "Conforme o poder e a decisão da mesa")
    };
}
