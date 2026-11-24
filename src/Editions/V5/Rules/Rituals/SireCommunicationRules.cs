using Game.Content;

namespace Game.Rules.V5.Rituals;

/// <summary>Explicit endings for the V5 Sire communication; elapsed time and disturbances come from the table.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), p. 277")]
public static class SireCommunicationRules
{
    public static string EndingEffect(SireCommunicationEnding ending) => ending switch
    {
        SireCommunicationEnding.TenMinutesElapsed =>
            "A mesa confirmou os dez minutos de comunicação; a conexão com o Senhor foi encerrada.",
        SireCommunicationEnding.Disturbed =>
            "A mesa confirmou uma perturbação em um dos lados; a comunicação com o Senhor foi interrompida.",
        _ => throw new ArgumentOutOfRangeException(nameof(ending))
    };
}

public enum SireCommunicationEnding { TenMinutesElapsed, Disturbed }
