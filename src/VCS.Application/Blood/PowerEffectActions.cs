using VCS.Content.Disciplines;

namespace VCS.Application.Blood;

public sealed record PowerEffectGuidance(string Description, int DamageBonus, bool IgnoresSuperficialReduction,
    bool CanUseObjects, bool CanSpeak, string Duration);

/// <summary>Mechanical flags for the core Protean forms; the catalog carries narrative effects for other powers.</summary>
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
        "power-protean-feral-weapons"
    };

    public PowerEffectGuidance Describe(PowerDefinition power) => power.Id switch
    {
        "power-protean-feral-weapons" => new(power.Summary, 2, true, true, true, "Uma cena"),
        "power-protean-shapechange" or "power-protean-metamorphosis" => new(power.Summary, 0, false, false, false, "Uma cena ou encerramento voluntário"),
        "power-protean-mist-form" => new(power.Summary, 0, false, false, false, "Uma cena ou encerramento voluntário"),
        _ => new(power.Summary, 0, false, true, true, "Conforme o poder e a decisão da mesa")
    };
}
