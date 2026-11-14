using VCS.Rules.V5.Blood;

namespace VCS.Desktop.Sheet;

public sealed record BloodSourceOption(BloodSource Source, string Label)
{
    public static IReadOnlyList<BloodSourceOption> All { get; } =
    [
        new(BloodSource.Human, "Humano"),
        new(BloodSource.Animal, "Animal"),
        new(BloodSource.Bagged, "Sangue ensacado")
    ];
}
