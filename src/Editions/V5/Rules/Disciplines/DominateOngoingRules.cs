using Game.Content;

namespace Game.Rules.V5.Disciplines;

/// <summary>Tracks the story-long target restriction caused by a total failure with Dominate.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), p. 255")]
public static class DominateOngoingRules
{
    public static bool IsDominatePower(string powerId) =>
        powerId.StartsWith("power-dominate-", StringComparison.Ordinal);

    public static bool CreatesStoryLock(string powerId, bool rolled, bool succeeded, int successes) =>
        IsDominatePower(powerId) && rolled && !succeeded && successes == 0;
}
