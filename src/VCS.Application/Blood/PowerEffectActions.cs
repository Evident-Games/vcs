using VCS.Content.Disciplines;

namespace VCS.Application.Blood;

public sealed record PowerEffectGuidance(string Description, int DamageBonus, bool IgnoresSuperficialReduction,
    bool CanUseObjects, bool CanSpeak, string Duration);

/// <summary>Mechanical flags for the core Protean forms; the catalog carries narrative effects for other powers.</summary>
public sealed class PowerEffectActions
{
    public PowerEffectGuidance Describe(PowerDefinition power) => power.Id switch
    {
        "power-protean-feral-weapons" => new(power.Summary, 2, true, true, true, "Uma cena"),
        "power-protean-shapechange" or "power-protean-metamorphosis" => new(power.Summary, 0, false, false, false, "Uma cena ou encerramento voluntário"),
        "power-protean-mist-form" => new(power.Summary, 0, false, false, false, "Uma cena ou encerramento voluntário"),
        _ => new(power.Summary, 0, false, true, true, "Conforme o poder e a decisão da mesa")
    };
}
