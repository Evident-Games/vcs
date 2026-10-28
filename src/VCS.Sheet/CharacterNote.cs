namespace VCS.Sheet;

/// <summary>One titled note of a character sheet.</summary>
public sealed record CharacterNote(Guid Id, string Title, string Text, DateTime UpdatedUtc);
