namespace VCS.Rules.V5.Blood;

public enum BloodSource
{
    Human,
    Animal,
    Bagged
}

public sealed record FeedingResult(
    BloodSource Source,
    int BloodPotency,
    decimal HungerBefore,
    decimal HungerAfter,
    decimal HungerSlaked,
    bool RequiresHumanDeathToReachCurrentHunger,
    bool CanProduceResonance,
    bool CanProduceDyscrasia,
    string RuleNote);

/// <summary>Resolves the core-book feeding limits by Blood Potency.</summary>
public static class FeedingRules
{
    public static FeedingResult Resolve(
        BloodSource source,
        int bloodPotency,
        decimal currentHunger,
        decimal requestedHungerSlake,
        bool humanWasDrainedToDeath = false,
        bool animalIsLarge = false,
        bool animalIsExceptional = false)
    {
        if (bloodPotency is < 0 or > 10) throw new ArgumentOutOfRangeException(nameof(bloodPotency));
        if (currentHunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(currentHunger));
        if (requestedHungerSlake < 0) throw new ArgumentOutOfRangeException(nameof(requestedHungerSlake));

        var maximum = source switch
        {
            BloodSource.Human => bloodPotency switch
            {
                >= 9 => 3m,
                >= 7 => 2m,
                _ => 1m
            },
            BloodSource.Animal when bloodPotency >= 3 && !animalIsExceptional => 0m,
            BloodSource.Animal when bloodPotency == 2 => animalIsLarge ? 1m : 0.5m,
            BloodSource.Animal => animalIsLarge ? 2m : 1m,
            BloodSource.Bagged when bloodPotency >= 3 => 0m,
            BloodSource.Bagged when bloodPotency == 2 => 0.5m,
            BloodSource.Bagged => 1m,
            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };

        var slaked = Math.Min(Math.Min(maximum, requestedHungerSlake), currentHunger);
        var threshold = bloodPotency >= 9 ? 3m : 2m;
        var deathRequiredForThreshold = source == BloodSource.Human && bloodPotency >= 5 &&
            currentHunger - slaked < threshold;
        if (deathRequiredForThreshold && !humanWasDrainedToDeath)
        {
            // Keep the character above the generation-independent killing threshold.
            slaked = Math.Min(slaked, Math.Max(0m, currentHunger - threshold));
        }

        var resonance = source == BloodSource.Human || source == BloodSource.Animal && (bloodPotency < 3 || animalIsExceptional);
        var dyscrasia = source == BloodSource.Human || source == BloodSource.Animal && animalIsExceptional;
        var note = source switch
        {
            BloodSource.Human when bloodPotency >= 9 => "Até 3 de Fome por humano; para ficar abaixo de 3, é preciso drenar e matar.",
            BloodSource.Human when bloodPotency >= 7 => "Até 2 de Fome por humano; para ficar abaixo de 2, é preciso drenar e matar.",
            BloodSource.Human when bloodPotency >= 5 => "Até 1 de Fome por humano; para ficar abaixo de 2, é preciso drenar e matar.",
            BloodSource.Animal when bloodPotency >= 3 && !animalIsExceptional => "A Potência do Sangue impede saciar Fome com sangue animal.",
            BloodSource.Animal when animalIsExceptional => "Animal excepcional: o Narrador decide os efeitos especiais; pode produzir Discrasia.",
            BloodSource.Animal when bloodPotency == 2 => "Sangue animal sacia meia Fome; animais excepcionais podem ser exceção.",
            BloodSource.Bagged when bloodPotency >= 3 => "Sangue ensacado não sacia Fome nesta Potência do Sangue.",
            BloodSource.Bagged when bloodPotency == 2 => "Sangue ensacado sacia meia Fome.",
            BloodSource.Bagged => "Sangue ensacado não concede Ressonância nem Discrasia.",
            _ => "Alimentação resolvida conforme a fonte e a Potência do Sangue."
        };

        return new(source, bloodPotency, currentHunger, currentHunger - slaked, slaked,
            deathRequiredForThreshold && !humanWasDrainedToDeath, resonance, dyscrasia, note);
    }
}
