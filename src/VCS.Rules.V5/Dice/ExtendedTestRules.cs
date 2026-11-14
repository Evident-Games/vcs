namespace VCS.Rules.V5.Dice;

public enum ExtendedTestStatus
{
    InProgress,
    Completed,
    Failed
}

public sealed record ExtendedTestState(
    int RequiredSuccesses,
    int AccumulatedSuccesses,
    int RollsMade,
    int MaximumRolls,
    ExtendedTestStatus Status);

public sealed record ExtendedTestStep(
    ExtendedTestState State,
    int SuccessesAdded,
    TestOutcome RollOutcome,
    bool ReachedGoal,
    bool ExhaustedIntervals);

/// <summary>Tracks accumulated successes and interval limits for V5 extended tests.</summary>
public static class ExtendedTestRules
{
    public static ExtendedTestState Start(int requiredSuccesses, int maximumRolls)
    {
        if (requiredSuccesses <= 0) throw new ArgumentOutOfRangeException(nameof(requiredSuccesses));
        if (maximumRolls <= 0) throw new ArgumentOutOfRangeException(nameof(maximumRolls));
        return new(requiredSuccesses, 0, 0, maximumRolls, ExtendedTestStatus.InProgress);
    }

    public static ExtendedTestStep AddRoll(ExtendedTestState state, DicePoolResult roll)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(roll);
        if (state.Status != ExtendedTestStatus.InProgress)
            throw new InvalidOperationException("Este teste estendido já foi encerrado.");
        if (state.RequiredSuccesses <= 0 || state.MaximumRolls <= 0 || state.RollsMade < 0 || state.AccumulatedSuccesses < 0)
            throw new ArgumentException("O estado do teste estendido é inválido.", nameof(state));

        var total = checked(state.AccumulatedSuccesses + roll.Successes);
        var rolls = checked(state.RollsMade + 1);
        var complete = total >= state.RequiredSuccesses;
        var exhausted = !complete && rolls >= state.MaximumRolls;
        var status = complete ? ExtendedTestStatus.Completed
            : exhausted ? ExtendedTestStatus.Failed
            : ExtendedTestStatus.InProgress;
        var next = state with { AccumulatedSuccesses = total, RollsMade = rolls, Status = status };
        return new(next, roll.Successes, roll.Outcome, complete, exhausted);
    }
}
