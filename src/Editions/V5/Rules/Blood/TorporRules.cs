namespace Game.Rules.V5.Blood;
using Game.Content;

public enum TorporDurationUnit
{
    Days,
    Weeks,
    Months,
    Years
}

public sealed record TorporDuration(int Humanity, int Amount, TorporDurationUnit Unit);

[RuleSource("Vampiro: A Máscara V5 (Livro base)")]
public static class TorporRules
{
    public static IReadOnlyList<TorporDuration> Durations { get; } =
    [
        new(10, 1, TorporDurationUnit.Days),
        new(9, 3, TorporDurationUnit.Days),
        new(8, 1, TorporDurationUnit.Weeks),
        new(7, 2, TorporDurationUnit.Weeks),
        new(6, 1, TorporDurationUnit.Months),
        new(5, 1, TorporDurationUnit.Years),
        new(4, 10, TorporDurationUnit.Years),
        new(3, 50, TorporDurationUnit.Years),
        new(2, 100, TorporDurationUnit.Years),
        new(1, 500, TorporDurationUnit.Years)
    ];

    public static TorporDuration ForHumanity(int humanity) =>
        Durations.SingleOrDefault(x => x.Humanity == humanity)
        ?? throw new ArgumentOutOfRangeException(nameof(humanity), "Humanidade deve ficar entre 1 e 10 para calcular Torpor.");

    public static DateTimeOffset NaturalWakeTime(int humanity, DateTimeOffset enteredAt)
    {
        var duration = ForHumanity(humanity);
        return duration.Unit switch
        {
            TorporDurationUnit.Days => enteredAt.AddDays(duration.Amount),
            TorporDurationUnit.Weeks => enteredAt.AddDays(duration.Amount * 7),
            TorporDurationUnit.Months => enteredAt.AddMonths(duration.Amount),
            TorporDurationUnit.Years => enteredAt.AddYears(duration.Amount),
            _ => throw new ArgumentOutOfRangeException(nameof(humanity))
        };
    }

    public static bool CanWakeNaturally(int humanity, DateTimeOffset enteredAt, DateTimeOffset now, bool staked)
    {
        if (staked) return false;
        return now >= NaturalWakeTime(humanity, enteredAt);
    }

    /// <summary>A vampire can be roused from Torpor by vitae of greater Blood Potency that slakes at least one Hunger.</summary>
    public static bool CanWakeWithVitae(int vampireBloodPotency, int vitaeBloodPotency, int hungerSlaked, bool staked = false)
    {
        if (vampireBloodPotency is < 0 or > 10) throw new ArgumentOutOfRangeException(nameof(vampireBloodPotency));
        if (vitaeBloodPotency is < 0 or > 10) throw new ArgumentOutOfRangeException(nameof(vitaeBloodPotency));
        if (hungerSlaked < 0) throw new ArgumentOutOfRangeException(nameof(hungerSlaked));
        return !staked && vitaeBloodPotency > vampireBloodPotency && hungerSlaked >= 1;
    }
}
