namespace VCS.Rules.V5.Blood;

public sealed record BloodBondState(string RegnantId, string ThrallId, int Level, DateOnly? LastVitaeDate)
{
    public bool IsComplete => Level >= 3;
}

public sealed record BloodBondDrinkResult(BloodBondState State, bool CountedAsNewDrink, bool BondCompleted);

public static class BloodBondRules
{
    /// <summary>Record no more than one bond-strengthening drink per night; three counted nights complete the bond.</summary>
    public static BloodBondDrinkResult RecordDrink(BloodBondState state, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (string.IsNullOrWhiteSpace(state.RegnantId)) throw new ArgumentException("RegnantId é obrigatório.", nameof(state));
        if (string.IsNullOrWhiteSpace(state.ThrallId)) throw new ArgumentException("ThrallId é obrigatório.", nameof(state));
        if (state.Level is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(state), "O nível do Laço deve ficar entre 0 e 3.");
        if (state.LastVitaeDate is { } last && date < last)
            throw new ArgumentOutOfRangeException(nameof(date), "A data do gole não pode anteceder o último gole registrado.");
        if (state.LastVitaeDate == date)
            return new(state, CountedAsNewDrink: false, BondCompleted: state.IsComplete);

        var level = LevelAt(state, date);
        var next = state with { Level = Math.Min(3, level + 1), LastVitaeDate = date };
        return new(next, CountedAsNewDrink: true, BondCompleted: next.IsComplete);
    }

    /// <summary>Bond strength decays by one level for each full month without a reinforcing drink.</summary>
    public static int LevelAt(BloodBondState state, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Level is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(state));
        if (state.LastVitaeDate is not { } last) return state.Level;
        if (date < last) throw new ArgumentOutOfRangeException(nameof(date));
        var months = (date.Year - last.Year) * 12 + date.Month - last.Month;
        if (date.Day < last.Day) months--;
        return Math.Max(0, state.Level - Math.Max(0, months));
    }
}
