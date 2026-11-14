using VCS.Rules.V5.Validation;
using VCS.Sheet;

namespace VCS.Rules.V5.Blood;

public sealed record HumanityLevel(int Level, string Description, int SocialPenalty = 0, int? BlushSocialPenalty = null);

public sealed record RemorseResult(CharacterDetails Details, IReadOnlyList<int> Dice, int Successes, bool MaintainedHumanity);

public static class HumanityRules
{
    /// <summary>Remorse pool equals current Humanity; one success clears all Stains, no successes loses one Humanity.</summary>
    public static RemorseResult ResolveRemorse(CharacterDetails details, IReadOnlyList<int> dice)
    {
        ArgumentNullException.ThrowIfNull(dice);
        if (details.Stains <= 0) throw new InvalidOperationException("Nao ha Maculas para resolver.");
        if (details.Humanity <= 0) throw new InvalidOperationException("Humanidade ja esta em zero (Wassail).");
        if (dice.Count != details.Humanity)
            throw new ArgumentException("A rolagem de Remorso usa uma quantidade de dados igual a Humanidade atual.", nameof(dice));
        if (dice.Any(face => face is < 1 or > 10))
            throw new ArgumentException("Cada dado deve resultar entre 1 e 10.", nameof(dice));

        var successes = dice.Count(face => face >= 6);
        var maintained = successes > 0;
        var updated = maintained
            ? details with { Stains = 0 }
            : details with { Stains = 0, Humanity = Math.Max(0, details.Humanity - 1) };
        return new(updated, dice.ToArray(), successes, maintained);
    }

    public static RemorseResult RollRemorse(CharacterDetails details, Random? random = null)
    {
        random ??= Random.Shared;
        var dice = Enumerable.Range(0, details.Humanity).Select(_ => random.Next(1, 11)).ToArray();
        return ResolveRemorse(details, dice);
    }

    public static IReadOnlyList<HumanityLevel> Levels { get; } =
    [
        new(10, "Aparência humana saudável sem Rubor da Vida; digere comida normalmente, permanece acordado de dia e sofre metade do dano do sol."),
        new(9, "Aparência doente, mas viva; cura ferimentos superficiais como humano; digere líquidos/carne crua e ganha uma hora extra acordado no sol."),
        new(8, "Dois dados para acionar Rubor da Vida; com o poder, sente prazer sexual real, digere vinhos e desperta uma hora antes."),
        new(7, "Moralidade padrão. Rubor da Vida exige teste normal; relações sexuais só podem ser fingidas por testes e reter comida requer extremo esforço."),
        new(6, "Apatia utilitária em mortes; rejeita comida forçada em uma hora; fingir atração sexual impõe -1 dado."),
        new(5, "Mutações inumanas leves, como a cor dos olhos; -1 dado em testes sociais mundanos e -2 dados para fingir atos sexuais.", -1),
        new(4, "Cadáver ambulante; -2 dados na socialização civil; impossível reter comidas ou líquidos.", -2),
        new(3, "Aparenta ser monstruoso sob qualquer iluminação; disfarce sexual impossível; -4 dados de penalidade social.", -4),
        new(2, "Trata mortais como gado e assusta todos no recinto; interação pacífica sofre -6 dados (-4 com Rubor).", -6, -4),
        new(1, "Mente fragmentada baseada em sangue e sono, sem emoções; abordagens empáticas e sociais sofrem -8 dados (-5 com Rubor).", -8, -5),
        new(0, "Wassail: perde totalmente o controle para a Besta e passa ao Narrador. Aviso de ficha encerrada na ficção; o aplicativo conserva os dados e não bloqueia edição.")
    ];
    public static HumanityLevel At(int level) => Levels.Single(x => x.Level == level);
    public static CharacterDetails ApplyStainDamage(CharacterDetails details, IReadOnlyList<TraitValue> traits)
    {
        if (details.Stains <= 0) throw new InvalidOperationException("Registre pelo menos uma Macula antes de aplicar sua consequencia.");
        var capacity = details.IsNpc ? details.NpcWillpowerCapacity : CharacterRules.WillpowerCapacity(traits);
        if (details.WillpowerSuperficial + details.WillpowerAggravated >= capacity)
            throw new InvalidOperationException("A trilha de Forca de Vontade nao possui espaco para 1 dano Agravado. Resolva os danos na mesa.");
        return details with { WillpowerAggravated = details.WillpowerAggravated + 1 };
    }
    public static CharacterDetails FailRemorse(CharacterDetails details)
    {
        if (details.Stains <= 0) throw new InvalidOperationException("Nao ha Maculas para resolver.");
        if (details.Humanity <= 0) throw new InvalidOperationException("Humanidade ja esta em zero (Wassail).");
        return details with { Stains = 0, Humanity = details.Humanity - 1 };
    }
}
