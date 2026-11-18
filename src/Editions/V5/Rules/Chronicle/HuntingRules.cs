using Game.Content;

namespace Game.Rules.V5.Chronicle;

public sealed record HuntingApproach(string Name, string Pool);

[RuleSource("Vampiro: A Máscara V5 (Livro base), pp. 306–308")]
public static class HuntingRules
{
    public static IReadOnlyList<HuntingApproach> Approaches { get; } =
    [
        new("Consensualista", "Manipulação + Persuasão"),
        new("Fazendeiro", "Autocontrole + Empatia com Animais"),
        new("Osíris", "Manipulação + Subterfúgio ou Intimidação + Fama"),
        new("Sacoleiro", "Inteligência + Manha"),
        new("Sandman", "Destreza + Furtividade"),
        new("Sanguessuga", "Manipulação + Subterfúgio; conduzir a cena com o Narrador"),
        new("Scene Queen", "Manipulação + Persuasão"),
        new("Sereia", "Carisma + Subterfúgio"),
        new("Trinchador", "Manipulação + Subterfúgio"),
        new("Vira-lata", "Força + Briga"),
        new("Outra abordagem", "Parada plausível aprovada pelo Narrador")
    ];

    public static int Difficulty(int territory, int securityModifier, int crowdModifier, int otherModifier)
    {
        if (territory is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(territory));
        if (securityModifier is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(securityModifier));
        if (crowdModifier is < -2 or > 0) throw new ArgumentOutOfRangeException(nameof(crowdModifier));
        if (otherModifier is < -20 or > 20) throw new ArgumentOutOfRangeException(nameof(otherModifier));
        return Math.Max(1, territory + securityModifier + crowdModifier + otherModifier);
    }
}
