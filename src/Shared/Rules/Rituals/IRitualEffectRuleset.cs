using Game.Content.Disciplines;

namespace Game.Rules.Rituals;

/// <summary>Presentation-neutral result; world facts are supplied by the Storyteller.</summary>
public sealed record RitualEffectProposal(bool Succeeded, string Effect, string Duration,
    int HealthDamage = 0, bool Aggravated = false, int Charges = 0, int PoolBonus = 0, int FollowDifficulty = 0);

public sealed record RitualRollContext(int Successes, int Difficulty, bool Critical);

/// <summary>Each edition owns its interpretation of a ritual roll. Only V5 is implemented.</summary>
public interface IRitualEffectRuleset
{
    string EditionId { get; }
    RitualEffectProposal Resolve(RitualUseDefinition ritual, RitualRollContext roll, int opponentSuccesses, bool vampireTarget);
    RitualEffectProposal CircleContact(int casterSuccesses, bool critical, int intruderSuccesses,
        bool attemptingToExit);
}
