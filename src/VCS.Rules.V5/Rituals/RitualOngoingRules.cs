using VCS.Content;

namespace VCS.Rules.V5.Rituals;

/// <summary>Context must be explicitly supplied by the table; never infer a social test or damage source.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), pp. 278–281")]
public static class RitualOngoingRules
{
    public const string NighthawkEndingSource = "Vampiro: A Máscara V5 (Livro base), p. 280";

    public static NighthawkEnding EndNighthawk(bool eyesRemoved) => eyesRemoved
        ? new(0, "Voo encerrado no fim da noite; a mesa confirmou que os olhos da ave foram removidos.")
        : new(3, "Voo encerrado no fim da noite; sem remover os olhos da ave, o conjurador fica cego pelas próximas três noites.");

    public static int AdvanceNighthawkBlindness(int nightsRemaining, DateOnly lastNight, DateOnly completedNight)
    {
        if (nightsRemaining is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(nightsRemaining));
        if (completedNight <= lastNight)
            throw new InvalidOperationException("Confirme o fim de uma noite posterior à última registrada. A mesma noite não pode ser contada duas vezes.");
        return Math.Max(0, nightsRemaining - (completedNight.DayNumber - lastNight.DayNumber));
    }
    public static int PoolModifier(bool heartOfStone, bool activeSocialTest, bool intimidation,
        bool dominate, bool resistingPresence) => !heartOfStone ? 0
        : resistingPresence ? 3 : activeSocialTest && !intimidation && !dominate ? -3 : 0;

    public static int FireDamage(int damage, bool firewalker, bool fire)
    {
        if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
        return firewalker && fire ? (damage + 1) / 2 : damage;
    }

    public static bool IgnoresDamage(bool incorporeal, bool ordinaryPhysicalSource,
        bool protectedByHaven, bool sunlight) => incorporeal && ordinaryPhysicalSource || protectedByHaven && sunlight;
}

public sealed record NighthawkEnding(int BlindnessNights, string Effect);
