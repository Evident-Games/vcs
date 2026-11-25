using Game.Content;

namespace Game.Rules.V5.Disciplines;

/// <summary>Tracks objective multi-night durations for core-book Obfuscate powers.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), p. 262")]
public static class ObfuscateOngoingRules
{
    public static int ConcealmentDurationNights(int margin) => 1 + Math.Max(0, margin);

    public static int AdvanceConcealmentNights(int nightsRemaining, DateOnly startedOn,
        DateOnly? lastNightAdvanced, DateOnly completedNight)
    {
        if (nightsRemaining <= 0)
            throw new InvalidOperationException("Este efeito de Ocultar não possui noites restantes.");
        if (lastNightAdvanced is { } previous && completedNight <= previous)
            throw new InvalidOperationException("Informe uma noite concluída posterior à última já registrada.");
        if (lastNightAdvanced is null && completedNight < startedOn)
            throw new InvalidOperationException("A noite concluída não pode ser anterior ao início do efeito.");
        var elapsed = lastNightAdvanced is { } previous
            ? completedNight.DayNumber - previous.DayNumber
            : completedNight.DayNumber - startedOn.DayNumber + 1;
        return Math.Max(0, nightsRemaining - elapsed);
    }
}
