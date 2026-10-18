namespace VCS.Domain.Characters;

public sealed record ExperienceCost(string Section, string Name, int Cost);
public sealed record ExperienceCalculation(int Total, int Spent, int CampaignSpent,
    IReadOnlyList<ExperienceCost> Costs, IReadOnlyList<ValidationIssue> Warnings);

public static class AutomaticExperience
{
    public static ExperienceCalculation Calculate(CharacterDetails details, IReadOnlyList<TraitValue> traits)
    {
        var state = details.Advancement ?? throw new InvalidOperationException("Idade e perfil de habilidades nao definidos.");
        var costs = new List<ExperienceCost>();
        var warnings = new List<ValidationIssue>();
        void Levels(string section, TraitValue trait, int free, int multiplier)
        {
            var cost = Enumerable.Range(1, Math.Max(0, trait.Rating))
                .Where(level => level > free).Sum(level => level * multiplier);
            if (cost > 0) costs.Add(new(section, trait.Name, cost));
        }
        void Distribution(TraitCategory category, int[] free, int multiplier, string section)
        {
            var items = traits.Where(x => x.Category == category).OrderByDescending(x => x.Rating)
                .ThenBy(x => x.Key, StringComparer.Ordinal).ToArray();
            for (var i = 0; i < items.Length; i++)
                Levels(section, items[i], i < free.Length ? free[i] : 0, multiplier);
        }
        if (!details.IsNpc)
            Distribution(TraitCategory.Attribute, [4, 3, 3, 3, 2, 2, 2, 2, 1], 5, "Attributes");
        Distribution(TraitCategory.Skill, CharacterAdvancement.SkillDistribution(state.Profile), 3, "Skills");

        var disciplines = traits.Where(x => x.Category == TraitCategory.Discipline)
            .Select(x => (Trait: x, Bonus: details.PredatorSelection?.DisciplineKey == x.Key ? 1 : 0))
            .Where(x => x.Trait.Rating > 0).OrderByDescending(x => x.Trait.Rating - x.Bonus)
            .ThenBy(x => x.Trait.Key, StringComparer.Ordinal).ToArray();
        var freeDiscipline = 0;
        foreach (var discipline in disciplines)
        {
            var inClan = DisciplineAdvancement.IsInClan(details.Clan, discipline.Trait.Name);
            if (inClan is null)
            {
                warnings.Add(new("Disciplines", $"Selecione um cla conhecido para calcular o XP de {discipline.Trait.Name}."));
                continue;
            }
            var free = inClan.Value && freeDiscipline < 2 ? (freeDiscipline++ == 0 ? 2 : 1) : 0;
            Levels("Disciplines", discipline.Trait, free + discipline.Bonus, inClan.Value ? 5 : 7);
        }
        var advantagePoints = traits.Where(x => x.Category == TraitCategory.Advantage && !PredatorRules.IsGrant(x)).Sum(x => x.Rating) +
            LoresheetRules.AdvantagePoints(traits);
        var advantageCost = Math.Max(0, advantagePoints - CharacterAdvancement.AdvantageBudget(state.Age)) * 3;
        if (advantageCost > 0) costs.Add(new("Advantages", "Vantagens acima dos pontos iniciais", advantageCost));
        var loresheetInitial = LoresheetRules.InitialXpCost(traits);
        var loresheetCampaign = traits.Where(x => x.Category == TraitCategory.Loresheet && x.Loresheet?.Payment == LoresheetPayment.CampaignXp)
            .Sum(x => x.Rating * 3);
        if (loresheetInitial + loresheetCampaign > 0)
            costs.Add(new("Advantages", "Loresheets pagos com XP", loresheetInitial + loresheetCampaign));
        var skills = traits.Where(x => x.Category == TraitCategory.Skill).ToArray();
        var specializationCount = skills.Sum(x => Math.Max(0,
            PredatorRules.SpecializationCount(details, x) - CharacterAdvancement.LocalFreeSpecializations(x)));
        var specializationCost = Math.Max(0, specializationCount - (details.PredatorSelection is not null || string.IsNullOrWhiteSpace(details.Predator) ? 1 : 2)) * 3;
        if (specializationCost > 0) costs.Add(new("Skills", "Especializacoes adicionais", specializationCost));
        if (skills.Any(x => x.Rating == 0 && CharacterAdvancement.SpecializationCount(x.Specialization) > 0))
            warnings.Add(new("Skills", "Especializacoes exigem pelo menos um ponto na habilidade."));
        var freeBlood = Math.Clamp(CharacterAdvancement.StartingBlood(state.Age) + (details.PredatorSelection?.BloodDelta ?? 0), 0, 10);
        Levels("State", new(TraitCategory.Attribute, "BloodPotency", "Potencia de Sangue", details.BloodPotency), freeBlood, 10);
        var total = CharacterAdvancement.StartingXp(state.Age) + details.CampaignExperienceTotal;
        var spent = costs.Sum(x => x.Cost);
        var campaignSpent = loresheetCampaign + Math.Max(0, spent - loresheetCampaign - CharacterAdvancement.StartingXp(state.Age));
        if (spent > total) warnings.Add(new("Background", $"Faltam {spent - total} XP para pagar os pontos da ficha."));
        if (loresheetCampaign > details.CampaignExperienceTotal)
            warnings.Add(new("Advantages", $"Faltam {loresheetCampaign - details.CampaignExperienceTotal} XP de campanha para os loresheets."));
        return new(total, spent, campaignSpent, costs, warnings);
    }
}
