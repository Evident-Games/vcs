using VCS.Rules.V5.Blood;

namespace VCS.Rules.V5.Dice;

public enum TestOutcome
{
    Failure,
    Success,
    CriticalSuccess,
    MessyCritical,
    BestialFailure
}

public sealed record DicePoolResult(
    IReadOnlyList<int> NormalDice,
    IReadOnlyList<int> HungerDice,
    int Successes,
    int Difficulty,
    int Tens,
    TestOutcome Outcome)
{
    public bool IsSuccess => Outcome is TestOutcome.Success or TestOutcome.CriticalSuccess or TestOutcome.MessyCritical;
    public bool IsCritical => Outcome is TestOutcome.CriticalSuccess or TestOutcome.MessyCritical;
    public int Margin => Successes - Difficulty;
}

/// <summary>V5 dice pool resolution. Supplying faces makes the rules deterministic and easy to test.</summary>
public static class DicePoolRules
{
    public static DicePoolResult Resolve(int pool, int hunger, int difficulty, IReadOnlyList<int> faces)
    {
        ArgumentNullException.ThrowIfNull(faces);
        if (pool < 0) throw new ArgumentOutOfRangeException(nameof(pool), "A parada não pode ser negativa.");
        if (hunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(hunger), "Fome deve ficar entre 0 e 5.");
        if (difficulty < 0) throw new ArgumentOutOfRangeException(nameof(difficulty), "A dificuldade não pode ser negativa.");
        if (faces.Count != pool) throw new ArgumentException("A quantidade de resultados deve corresponder à parada.", nameof(faces));
        if (faces.Any(face => face is < 1 or > 10)) throw new ArgumentException("Cada dado deve resultar entre 1 e 10.", nameof(faces));

        var hungerCount = Math.Min(hunger, pool);
        var hungerDice = faces.Take(hungerCount).ToArray();
        var normalDice = faces.Skip(hungerCount).ToArray();
        var successes = faces.Sum(face => face >= 6 ? face == 10 ? 2 : 1 : 0);
        var tens = faces.Count(face => face == 10);
        var won = successes >= difficulty;
        var critical = won && tens >= 2;
        var messy = critical && hungerDice.Contains(10);
        var bestial = !won && hungerDice.Contains(1);

        var outcome = bestial ? TestOutcome.BestialFailure
            : messy ? TestOutcome.MessyCritical
            : critical ? TestOutcome.CriticalSuccess
            : won ? TestOutcome.Success
            : TestOutcome.Failure;

        return new(normalDice, hungerDice, successes, difficulty, tens, outcome);
    }

    public static DicePoolResult Roll(int pool, int hunger, int difficulty, Random? random = null)
    {
        random ??= Random.Shared;
        var faces = Enumerable.Range(0, pool).Select(_ => random.Next(1, 11)).ToArray();
        return Resolve(pool, hunger, difficulty, faces);
    }

    /// <summary>Willpower rerolls can replace failed regular dice only; Hunger dice are never rerolled.</summary>
    public static DicePoolResult RerollRegularFailures(
        int pool, int hunger, int difficulty, IReadOnlyList<int> initialFaces,
        IReadOnlyList<int> rerolledRegularDieIndices, IReadOnlyList<int> replacementFaces)
    {
        ArgumentNullException.ThrowIfNull(initialFaces);
        ArgumentNullException.ThrowIfNull(rerolledRegularDieIndices);
        ArgumentNullException.ThrowIfNull(replacementFaces);
        var initial = Resolve(pool, hunger, difficulty, initialFaces);
        if (rerolledRegularDieIndices.Count != replacementFaces.Count)
            throw new ArgumentException("Cada dado rerrolado precisa de um resultado substituto.");
        if (rerolledRegularDieIndices.Count > 3)
            throw new ArgumentException("Força de Vontade permite rerrolar até três dados comuns.");
        var hungerCount = Math.Min(hunger, pool);
        var faces = initialFaces.ToArray();
        var seen = new HashSet<int>();
        for (var i = 0; i < rerolledRegularDieIndices.Count; i++)
        {
            var index = rerolledRegularDieIndices[i];
            if (index < hungerCount || index >= pool)
                throw new ArgumentOutOfRangeException(nameof(rerolledRegularDieIndices), "Dados de Fome não podem ser rerrolados.");
            if (!seen.Add(index)) throw new ArgumentException("Um dado não pode ser rerrolado mais de uma vez.");
            if (faces[index] >= 6) throw new ArgumentException("Só dados comuns que falharam podem ser rerrolados.");
            faces[index] = replacementFaces[i];
        }
        return Resolve(pool, hunger, difficulty, faces);
    }
}

public sealed record RouseCheckResult(int Die, bool Succeeded, int HungerBefore, int HungerAfter, int? RerolledDie = null)
{
    public bool IncreasedHunger => HungerAfter > HungerBefore;
}

public static class RouseCheckRules
{
    public static RouseCheckResult Resolve(int hunger, int die)
    {
        if (hunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(hunger));
        if (die is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(die));
        var succeeded = die >= 6;
        return new(die, succeeded, hunger, succeeded ? hunger : Math.Min(5, hunger + 1));
    }

    public static bool CanRerollForDisciplinePower(int bloodPotency, int powerLevel) =>
        powerLevel > 0 && powerLevel <= BloodPotencyEffects.At(bloodPotency).RouseRerollLevel;

    public static RouseCheckResult ResolveForDisciplinePower(
        int hunger, int bloodPotency, int powerLevel, int die, int? rerolledDie = null)
    {
        var first = Resolve(hunger, die);
        if (!CanRerollForDisciplinePower(bloodPotency, powerLevel) || first.Succeeded || rerolledDie is null)
            return first;
        var second = Resolve(hunger, rerolledDie.Value);
        return second with { RerolledDie = rerolledDie.Value };
    }

    public static RouseCheckResult Roll(int hunger, Random? random = null)
    {
        random ??= Random.Shared;
        return Resolve(hunger, random.Next(1, 11));
    }
}
