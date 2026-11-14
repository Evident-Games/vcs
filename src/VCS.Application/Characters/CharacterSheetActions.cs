using VCS.Rules.V5.Advancement;
using VCS.Rules.V5.Blood;
using VCS.Rules.V5.Validation;
using VCS.Rules.V5.Clans;
using VCS.Sheet;

namespace VCS.Application.Characters;

public sealed record BloodPotencyWarning(int Generation, int BloodPotency, int Minimum, int Maximum, bool HasDefinedRange, bool IsThinBlood);

/// <summary>Coordinates common character-sheet calculations and V5 validation for any presentation client.</summary>
public sealed class CharacterSheetActions
{
    public IReadOnlyList<int> AttributeDistribution => CharacterAdvancement.AttributeDistribution;
    public IReadOnlyList<int> DisciplineDistribution => CharacterAdvancement.DisciplineDistribution;

    public IReadOnlyList<ValidationIssue> Validate(CharacterDetails details, IReadOnlyList<TraitValue> traits) =>
        CharacterRules.Validate(details, traits);

    public int HealthCapacity(IEnumerable<TraitValue> traits) => CharacterRules.HealthCapacity(traits);
    public int WillpowerCapacity(IEnumerable<TraitValue> traits) => CharacterRules.WillpowerCapacity(traits);

    public ExperienceCalculation CalculateExperience(CharacterDetails details, IReadOnlyList<TraitValue> traits) =>
        AutomaticExperience.Calculate(details, traits);

    public int StartingExperience(VampireAge age) => CharacterAdvancement.StartingXp(age);
    public int StartingBlood(VampireAge age) => CharacterAdvancement.StartingBlood(age);
    public int AdvantageBudget(VampireAge age) => CharacterAdvancement.AdvantageBudget(age);
    public int FlawBudget(VampireAge age) => CharacterAdvancement.FlawBudget(age);
    public int AdvantagePoints(IEnumerable<TraitValue> traits) => CharacterAdvancement.AdvantagePoints(traits);
    public int FlawPoints(IEnumerable<TraitValue> traits) => CharacterAdvancement.FlawPoints(traits);
    public IReadOnlyList<int> SkillDistribution(SkillProfile profile) => CharacterAdvancement.SkillDistribution(profile);
    public IReadOnlyList<int> MissingDistribution(IEnumerable<int> ratings, IEnumerable<int> distribution) =>
        CharacterAdvancement.MissingDistribution(ratings, distribution);
    public int FreeSpecializationsRemaining(CharacterDetails details, IEnumerable<TraitValue> traits) =>
        CharacterAdvancement.FreeSpecializationsRemaining(details, traits);
    public bool? IsDisciplineInClan(string clan, string discipline) => DisciplineAdvancement.IsInClan(clan, discipline);

    public AdvancementState SyncAttributeBase(AdvancementState state, IEnumerable<TraitValue> attributes) =>
        CharacterAdvancement.SyncAttributeBase(state, attributes);

    public AdvancementState SyncDisciplineBase(AdvancementState state, CharacterDetails details, IEnumerable<TraitValue> disciplines) =>
        CharacterAdvancement.SyncDisciplineBase(state, details, disciplines);

    public BloodPotencyWarning CheckBloodPotency(int generation, int bloodPotency)
    {
        var range = BloodPotencyGenerationRules.ForGeneration(generation);
        return new(generation, bloodPotency, range?.Minimum ?? 0, range?.Maximum ?? 0,
            range is not null, BloodPotencyGenerationRules.IsThinBloodGeneration(generation));
    }
}
