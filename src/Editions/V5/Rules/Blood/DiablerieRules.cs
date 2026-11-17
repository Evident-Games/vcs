using VCS.Content;

namespace VCS.Rules.V5.Blood;

public sealed record DiablerieResolution(int Humanity, int Generation, int RestrictedExperience, bool Possessed, int AuraYears);

[RuleSource("Vampiro: A Máscara V5 (Livro base), pp. 234–235")]
public static class DiablerieRules
{
    /// <summary>Resolves consequences after the victim's soul has been consumed and the opposed test resolved.</summary>
    public static DiablerieResolution Resolve(int humanity, int generation, int victimGeneration,
        int ownSuccesses, int victimSuccesses)
    {
        if (humanity is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(humanity));
        if (generation is < 3 or > 16 || victimGeneration is < 3 or > 16)
            throw new ArgumentOutOfRangeException(nameof(generation));
        if (ownSuccesses < 0 || victimSuccesses < 0) throw new ArgumentOutOfRangeException(nameof(ownSuccesses));
        var remainingHumanity = Math.Max(0, humanity - 1 - Math.Max(0, victimSuccesses - ownSuccesses));
        return new(remainingHumanity, victimGeneration < generation ? generation - 1 : generation,
            checked(ownSuccesses * 5), remainingHumanity == 0, Math.Max(1, generation - victimGeneration));
    }
}
