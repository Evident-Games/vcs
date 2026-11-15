using VCS.Content;

namespace VCS.Rules.V5.Rituals;

/// <summary>Effects of the awakening ritual after the danger-triggered roll.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), p. 276")]
public static class AwakeningRitualRules
{
    public static string Describe(bool success, bool critical) => !success
        ? "O ritual não desperta o personagem; o Narrador decide as consequências."
        : critical
            ? "Desperta alerta e ignora as penalidades para permanecer acordado durante o dia até o próximo amanhecer."
            : "Desperta alerta e ignora as penalidades para permanecer acordado durante o dia enquanto durar a cena.";
}
