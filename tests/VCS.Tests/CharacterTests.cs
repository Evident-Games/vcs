using VCS.Domain.Characters;

namespace VCS.Tests;

public sealed class CharacterTests
{
    [Fact]
    public void New_character_has_valid_defaults_and_utc_timestamps()
    {
        var character = Character.Create(new() { Name = "  Alice  " });
        Assert.Equal("Alice", character.Details.Name);
        Assert.Equal(4, character.HealthCapacity);
        Assert.Equal(2, character.WillpowerCapacity);
        Assert.Equal(DateTimeKind.Utc, character.CreatedUtc.Kind);
        Assert.Equal(1, character.Revision);
    }

    [Fact]
    public void Capacities_follow_attributes()
    {
        var traits = CharacterRules.DefaultAttributes.Select(t => t with
        {
            Rating = t.Key switch { "Stamina" => 4, "Composure" => 3, "Resolve" => 5, _ => 1 }
        }).ToArray();
        var character = Character.Create(new(), traits);
        Assert.Equal(7, character.HealthCapacity);
        Assert.Equal(8, character.WillpowerCapacity);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void Hunger_outside_track_is_rejected(int hunger) =>
        Assert.Throws<CharacterValidationException>(() => Character.Create(new() { Hunger = hunger }));

    [Fact]
    public void Invalid_update_leaves_original_untouched()
    {
        var character = Character.Create(new());
        Assert.Throws<CharacterValidationException>(() => character.Update(
            character.Details with { Name = "", HealthAggravated = 5 }, CharacterRules.DefaultAttributes));
        Assert.Equal("Novo personagem", character.Details.Name);
        Assert.Equal(1, character.Revision);
    }

    [Fact]
    public void Duplicate_has_independent_identity_and_traits()
    {
        var original = Character.Create(new() { Notes = "Notas pessoais" });
        var copy = original.Duplicate();
        Assert.NotEqual(original.Id, copy.Id);
        Assert.Equal("Notas pessoais", copy.Details.Notes);
        Assert.EndsWith("(copia)", copy.Details.Name);
        Assert.DoesNotContain(copy.Traits.First().Id, original.Traits.Select(x => x.Id));
    }

    [Fact]
    public void Missing_or_duplicate_attributes_are_rejected()
    {
        Assert.Throws<CharacterValidationException>(() => Character.Create(new(), []));
        Assert.Throws<CharacterValidationException>(() => Character.Create(new(),
            [.. CharacterRules.DefaultAttributes, CharacterRules.DefaultAttributes[0]]));
    }

    [Fact]
    public void Total_damage_cannot_exceed_capacity() =>
        Assert.Throws<CharacterValidationException>(() => Character.Create(
            new() { HealthSuperficial = 3, HealthAggravated = 2 }));

    [Fact]
    public void Track_boundaries_and_all_damage_slots_are_accepted()
    {
        var character = Character.Create(new()
        {
            Generation = 16, Hunger = 5, Humanity = 10, BloodPotency = 10,
            HealthSuperficial = 2, HealthAggravated = 2, WillpowerAggravated = 2
        });
        Assert.Equal(4, character.HealthCapacity);
        Assert.Empty(CharacterRules.Validate(character.Details, character.Traits.Select(x => x.ToValue()).ToArray()));
    }

    [Fact]
    public void Invalid_ranges_enum_and_text_are_reported_together()
    {
        var issues = CharacterRules.Validate(new()
        {
            Name = new string('x', 121), Generation = 0, Humanity = 11, BloodPotency = -1,
            WillpowerSuperficial = -1
        }, [.. CharacterRules.DefaultAttributes, new((TraitCategory)100, "", "", 6)]);
        Assert.Contains(issues, x => x.Field == "Name");
        Assert.Contains(issues, x => x.Field == "Generation");
        Assert.Contains(issues, x => x.Field == "Humanity");
        Assert.Contains(issues, x => x.Field == "BloodPotency");
        Assert.Contains(issues, x => x.Field == "Willpower");
        Assert.Contains(issues, x => x.Field == "Traits");
    }

    [Fact]
    public void Experience_and_extended_texts_are_validated()
    {
        Assert.Throws<CharacterValidationException>(() => Character.Create(new() { ExperienceTotal = 10, ExperienceSpent = 11 }));
        Assert.Throws<CharacterValidationException>(() => Character.Create(new() { ExperienceTotal = -1 }));
        Assert.Throws<CharacterValidationException>(() => Character.Create(new() { ExperienceSpent = -1 }));
        Assert.Throws<CharacterValidationException>(() => Character.Create(new() { Ambition = new string('x', 501) }));
        Assert.Throws<CharacterValidationException>(() => Character.Create(new() { History = new string('x', 20001) }));
        Assert.Equal(10, Character.Create(new() { ExperienceTotal = 10, ExperienceSpent = 10 }).Details.ExperienceSpent);
    }
}
