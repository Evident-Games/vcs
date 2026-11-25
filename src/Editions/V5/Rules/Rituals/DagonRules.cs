using Game.Content;

namespace Game.Rules.V5.Rituals;

/// <summary>Limits and completion text for the optional repetitions of Dagon's Call.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), p. 278")]
public static class DagonRules
{
    public const int MaximumExecutions = 3;

    public static void ValidatePrerequisite(bool bloodExposureConfirmed, bool oneHourElapsedConfirmed,
        bool withinOneWeekConfirmed, bool exceptionApproved)
    {
        if (exceptionApproved) return;
        if (!bloodExposureConfirmed)
            throw new InvalidOperationException("Confirme que a vítima foi exposta ao Sangue do conjurador antes de prosseguir.");
        if (!oneHourElapsedConfirmed)
            throw new InvalidOperationException("Confirme que ao menos uma hora passou desde a exposição ao Sangue.");
        if (!withinOneWeekConfirmed)
            throw new InvalidOperationException("Confirme que ainda não passou uma semana desde a exposição ao Sangue.");
    }

    public static string CompletionEffect(int executions)
    {
        if (executions is < 1 or >= MaximumExecutions)
            throw new ArgumentOutOfRangeException(nameof(executions));

        var executionLabel = executions == 1 ? "uma execução" : $"{executions} execuções";
        return $"A mesa confirmou o encerramento de O Chamado de Dagon após {executionLabel}; " +
            "o conjurador não realizará outra repetição deste preparo.";
    }
}
