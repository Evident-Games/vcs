using Game.Content.Disciplines;

namespace Game.Application.Blood;

public sealed record PowerEffectGuidance(string Description, int DamageBonus, bool IgnoresSuperficialReduction,
    bool CanUseObjects, bool CanSpeak, string Duration);

/// <summary>Core powers with a direct sheet calculation or an explicit application of their main numeric effect. Secondary and narrative rules can still require Storyteller adjudication.</summary>
public sealed class PowerEffectActions
{
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
