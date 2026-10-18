namespace VCS.Domain.Characters;

public enum VampireAge { Child, Neonate, Ancilla }
public enum SkillProfile { Specialist, Balanced, Generalist }

public sealed record ExperiencePurchase(DateTime Utc, TraitCategory? Category, string Key, string Name,
    int From, int To, int Cost, string Specialization = "", string Clan = "", int CampaignCost = 0, LoresheetInfo? Loresheet = null);

public sealed record AdvancementState
{
    public bool AutomaticExperience { get; init; }
    public bool IsCreating { get; init; } = true;
    public bool HasCreationProfile { get; init; } = true;
    public VampireAge Age { get; init; }
    public SkillProfile Profile { get; init; }
    public TraitValue[] Baseline { get; init; } = [];
    public int BaselineBloodPotency { get; init; }
    public PredatorSelection? BaselinePredatorSelection { get; init; }
    public int BaselineSpent { get; init; }
    public int BaselineCampaignSpent { get; init; }
    public int BaselineNpcPhysical { get; init; }
    public int BaselineNpcSocial { get; init; }
    public int BaselineNpcMental { get; init; }
    public ExperiencePurchase[] Purchases { get; init; } = [];

    public bool Equals(AdvancementState? other) => other is not null &&
        AutomaticExperience == other.AutomaticExperience && IsCreating == other.IsCreating && HasCreationProfile == other.HasCreationProfile && Age == other.Age && Profile == other.Profile &&
        BaselineBloodPotency == other.BaselineBloodPotency && BaselinePredatorSelection == other.BaselinePredatorSelection &&
        BaselineSpent == other.BaselineSpent && BaselineCampaignSpent == other.BaselineCampaignSpent &&
        BaselineNpcPhysical == other.BaselineNpcPhysical && BaselineNpcSocial == other.BaselineNpcSocial &&
        BaselineNpcMental == other.BaselineNpcMental && Baseline.SequenceEqual(other.Baseline) && Purchases.SequenceEqual(other.Purchases);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(AutomaticExperience);
        hash.Add(IsCreating); hash.Add(HasCreationProfile); hash.Add(Age); hash.Add(Profile); hash.Add(BaselineBloodPotency); hash.Add(BaselineSpent);
        hash.Add(BaselineCampaignSpent);
        hash.Add(BaselinePredatorSelection);
        hash.Add(BaselineNpcPhysical); hash.Add(BaselineNpcSocial); hash.Add(BaselineNpcMental);
        foreach (var value in Baseline) hash.Add(value);
        foreach (var value in Purchases) hash.Add(value);
        return hash.ToHashCode();
    }
}

public sealed record AdvancementResult(CharacterDetails Details, IReadOnlyList<TraitValue> Traits);

public static class CharacterAdvancement
{
    public static int StartingXp(VampireAge age) => age switch
    {
        VampireAge.Child => 0, VampireAge.Neonate => 15, VampireAge.Ancilla => 35,
        _ => throw new ArgumentOutOfRangeException(nameof(age))
    };
    public static int StartingBlood(VampireAge age) => age switch
    {
        VampireAge.Child => 0, VampireAge.Neonate => 1, VampireAge.Ancilla => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(age))
    };
    public static int AdvantageBudget(VampireAge age) => age == VampireAge.Ancilla ? 9 : 7;
    public static int FlawBudget(VampireAge age) => age == VampireAge.Ancilla ? 4 : 2;
    public static int[] SkillDistribution(SkillProfile profile) => profile switch
    {
        SkillProfile.Specialist => [4, 3, 3, 3, 2, 2, 2, 2, 1],
        SkillProfile.Balanced => [3, 3, 3, 2, 2, 2, 2, 2, 1],
        SkillProfile.Generalist => [4, 3, 2, 2, 2, 2, 2, 1, 1],
        _ => throw new ArgumentOutOfRangeException(nameof(profile))
    };

    public static int SpecializationCount(string text) =>
        text.Split([',', ';', '\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length;

    public static int LocalFreeSpecializations(TraitValue skill) =>
        (skill.Rating >= 4 ? 1 : 0) +
        (skill.Rating >= 1 && new[] { "Erudição", "Acadêmicos", "Academics", "Ofícios", "Artesanato", "Craft",
            "Performance", "Ciência", "Science" }.Any(x => ClanCatalog.SameName(x, skill.Name)) ? 1 : 0);

    private static int GeneralSpecializations(CharacterDetails details) =>
        details.PredatorSelection is not null || string.IsNullOrWhiteSpace(details.Predator) ? 1 : 2;

    public static int FreeSpecializationsRemaining(CharacterDetails details, IEnumerable<TraitValue> traits)
    {
        var paid = details.Advancement?.AutomaticExperience == true ? 0 :
            details.Advancement?.Purchases.Count(x => x.Specialization.Length != 0 && x.Cost > 0) ?? 0;
        var values = traits.Where(x => x.Category == TraitCategory.Skill).ToArray();
        var local = values.Sum(x => Math.Max(0, LocalFreeSpecializations(x) - PredatorRules.SpecializationCount(details, x)));
        var usedGeneral = values.Sum(x => Math.Max(0, PredatorRules.SpecializationCount(details, x) - LocalFreeSpecializations(x)));
        return local + Math.Max(0, GeneralSpecializations(details) + paid - usedGeneral);
    }

    public static IReadOnlyList<string> CreationIssues(CharacterDetails details, IReadOnlyList<TraitValue> traits)
    {
        var state = details.Advancement ?? throw new InvalidOperationException("Ative as regras de criacao.");
        var issues = new List<string>();
        issues.AddRange(LoresheetRules.Validate(details, traits).Select(x => x.Message));
        issues.AddRange(PredatorRules.Validate(details, traits).Select(x => x.Message));
        if (!details.IsNpc && !traits.Where(x => x.Category == TraitCategory.Attribute).Select(x => x.Rating)
                .OrderDescending().SequenceEqual(new[] { 4, 3, 3, 3, 2, 2, 2, 2, 1 }))
            issues.Add("Atributos: distribua 1 x 4, 3 x 3, 4 x 2 e 1 x 1.");
        if (!traits.Where(x => x.Category == TraitCategory.Skill && x.Rating > 0).Select(x => x.Rating)
                .OrderDescending().SequenceEqual(SkillDistribution(state.Profile)))
            issues.Add("Habilidades: a distribuicao nao corresponde ao perfil escolhido; as demais ficam em zero.");
        var disciplines = traits.Where(x => x.Category == TraitCategory.Discipline)
            .Select(x => details.PredatorSelection?.DisciplineKey == x.Key ? x with { Rating = x.Rating - 1 } : x)
            .Where(x => x.Rating > 0).ToArray();
        if (!disciplines.Select(x => x.Rating).OrderDescending().SequenceEqual(new[] { 2, 1 }) ||
            disciplines.Any(x => DisciplineAdvancement.IsInClan(details.Clan, x.Name) != true))
            issues.Add("Disciplinas: escolha duas do cla, uma com 2 pontos e outra com 1.");
        if (traits.Where(x => x.Category == TraitCategory.Advantage && !PredatorRules.IsGrant(x)).Sum(x => x.Rating) + LoresheetRules.AdvantagePoints(traits) != AdvantageBudget(state.Age))
            issues.Add($"Vantagens: distribua {AdvantageBudget(state.Age)} pontos.");
        if (traits.Where(x => x.Category == TraitCategory.Flaw && !PredatorRules.IsGrant(x)).Sum(x => x.Rating) != FlawBudget(state.Age))
            issues.Add($"Desvantagens: distribua {FlawBudget(state.Age)} pontos.");
        if (details.BloodPotency != StartingBlood(state.Age) + (details.PredatorSelection?.BloodDelta ?? 0))
            issues.Add($"Potencia de Sangue inicial: {StartingBlood(state.Age) + (details.PredatorSelection?.BloodDelta ?? 0)} (inclui predador).");
        if (details.ExperienceTotal < StartingXp(state.Age))
            issues.Add($"O XP total inicial deve incluir o bonus de {StartingXp(state.Age)} XP.");
        if (details.ExperienceSpent != LoresheetRules.InitialXpCost(traits) || state.Purchases.Length != 0)
            issues.Add("Pontos iniciais sao gratuitos; compre evolucoes depois de concluir a criacao.");
        var excess = traits.Where(x => x.Category == TraitCategory.Skill)
            .Sum(x => Math.Max(0, PredatorRules.SpecializationCount(details, x) - LocalFreeSpecializations(x)));
        if (traits.Any(x => x.Category == TraitCategory.Skill && x.Rating == 0 && SpecializationCount(x.Specialization) > 0))
            issues.Add("Especializacoes exigem pelo menos um ponto na habilidade.");
        if (excess > GeneralSpecializations(details))
            issues.Add("Especializacoes excedem as gratuitas. Separe cada uma por ponto e virgula; compras custam 3 XP apos a criacao.");
        return issues;
    }

    public static CharacterDetails FinalizeCreation(CharacterDetails details, IReadOnlyList<TraitValue> traits)
    {
        if (details.Advancement?.IsCreating != true) throw new InvalidOperationException("Esta ficha nao esta em criacao.");
        var issues = CreationIssues(details, traits);
        if (issues.Count != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, issues));
        return EstablishBaseline(details, traits);
    }

    public static CharacterDetails EstablishBaseline(CharacterDetails details, IReadOnlyList<TraitValue> traits) =>
        details with
        {
            Advancement = (details.Advancement ?? new() { HasCreationProfile = false }) with
            {
                IsCreating = false, Baseline = traits.ToArray(), BaselineBloodPotency = details.BloodPotency,
                BaselinePredatorSelection = details.PredatorSelection,
                BaselineSpent = details.ExperienceSpent, Purchases = [],
                BaselineCampaignSpent = details.CampaignExperienceSpent,
                BaselineNpcPhysical = details.NpcPhysical, BaselineNpcSocial = details.NpcSocial, BaselineNpcMental = details.NpcMental
            }
        };

    public static int NextCost(CharacterDetails details, TraitValue? trait)
    {
        if (trait?.Category == TraitCategory.Loresheet)
            throw new InvalidOperationException("Beneficios de loresheet sao independentes; use o catalogo de loresheets.");
        var rating = trait?.Rating ?? details.BloodPotency;
        if (rating >= (trait is null ? 10 : 5)) throw new InvalidOperationException("Nivel maximo atingido.");
        return trait?.Category switch
        {
            null => (rating + 1) * 10,
            TraitCategory.Attribute => (rating + 1) * 5,
            TraitCategory.Skill => (rating + 1) * 3,
            TraitCategory.Discipline => DisciplineAdvancement.NextLevelCost(details.Clan, trait.Name, rating)
                ?? throw new InvalidOperationException("Selecione um cla conhecido para calcular o custo da disciplina."),
            TraitCategory.Advantage => 3,
            _ => throw new InvalidOperationException("Esta categoria nao possui compra de XP.")
        };
    }

    public static AdvancementResult Buy(CharacterDetails details, IReadOnlyList<TraitValue> traits,
        TraitCategory? category, string key, string specialization = "", string newTraitName = "")
    {
        var state = details.Advancement;
        if (state is null || state.IsCreating) throw new InvalidOperationException("Conclua a criacao ou registre a base da ficha antes de comprar.");
        if (state.Purchases.Length >= 2000) throw new InvalidOperationException("Limite de 2000 compras atingido.");
        if (category == TraitCategory.Loresheet)
            throw new InvalidOperationException("Use o catalogo de loresheets para comprar o beneficio inteiro.");
        if (category is null && key != "BloodPotency")
        {
            if (!details.IsNpc || key is not ("NpcPhysical" or "NpcSocial" or "NpcMental") || specialization.Length != 0)
                throw new InvalidOperationException("Atributo agrupado de NPC invalido.");
            var rating = key == "NpcPhysical" ? details.NpcPhysical : key == "NpcSocial" ? details.NpcSocial : details.NpcMental;
            if (rating >= 5) throw new InvalidOperationException("Nivel maximo atingido.");
            var price = (rating + 1) * 5;
            if (details.ExperienceTotal - details.ExperienceSpent < price) throw new InvalidOperationException($"XP insuficiente. Custo: {price} XP.");
            var label = key == "NpcPhysical" ? "Fisicos" : key == "NpcSocial" ? "Sociais" : "Mentais";
            return new(details with
            {
                NpcPhysical = details.NpcPhysical + (key == "NpcPhysical" ? 1 : 0),
                NpcSocial = details.NpcSocial + (key == "NpcSocial" ? 1 : 0),
                NpcMental = details.NpcMental + (key == "NpcMental" ? 1 : 0),
                ExperienceSpent = details.ExperienceSpent + price,
                CampaignExperienceSpent = details.CampaignExperienceSpent + LoresheetRules.CampaignCost(details, price),
                Advancement = state with { Purchases = [.. state.Purchases,
                    new(DateTime.UtcNow, null, key, label, rating, rating + 1, price, CampaignCost: LoresheetRules.CampaignCost(details, price))] }
            }, traits);
        }
        var values = traits.ToArray();
        var index = category is null ? -1 : Array.FindIndex(values, x => x.Category == category && x.Key == key);
        if (index < 0 && category is TraitCategory.Advantage or TraitCategory.Discipline && !string.IsNullOrWhiteSpace(newTraitName))
        {
            index = values.Length;
            values = [.. values, new(category.Value, key, newTraitName, 0)];
        }
        if (category is not null && index < 0) throw new InvalidOperationException("Selecione um traco existente na ficha.");
        var trait = index < 0 ? null : values[index];
        specialization = specialization.Trim();
        var cost = 0;
        if (specialization.Length > 0)
        {
            if (trait?.Category != TraitCategory.Skill) throw new InvalidOperationException("Selecione uma habilidade para especializar.");
            if (trait.Rating < 1) throw new InvalidOperationException("A habilidade precisa ter pelo menos um ponto.");
            if (SpecializationCount(specialization) != 1) throw new InvalidOperationException("Adicione uma especializacao por vez.");
            var existing = trait.Specialization.Split([',', ';', '\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (existing.Any(x => ClanCatalog.SameName(x, specialization))) throw new InvalidOperationException("Esta especializacao ja existe.");
            var combined = string.IsNullOrWhiteSpace(trait.Specialization) ? specialization : trait.Specialization + "; " + specialization;
            if (combined.Length > 500) throw new InvalidOperationException("Especializacoes: limite de 500 caracteres.");
            var paid = state.Purchases.Count(x => x.Specialization.Length != 0 && x.Cost > 0);
            var usedGeneral = traits.Where(x => x.Category == TraitCategory.Skill)
                .Sum(x => Math.Max(0, PredatorRules.SpecializationCount(details, x) - LocalFreeSpecializations(x)));
            var free = PredatorRules.SpecializationCount(details, trait) < LocalFreeSpecializations(trait) ||
                usedGeneral < GeneralSpecializations(details) + paid;
            cost = free ? 0 : 3;
            values[index] = trait with { Specialization = combined };
        }
        else
        {
            cost = NextCost(details, trait);
            if (trait is not null) values[index] = trait with { Rating = trait.Rating + 1 };
        }
        if (details.ExperienceTotal - details.ExperienceSpent < cost)
            throw new InvalidOperationException($"XP insuficiente. Custo: {cost} XP.");
        var from = trait?.Rating ?? details.BloodPotency;
        var purchase = new ExperiencePurchase(DateTime.UtcNow, category, trait?.Key ?? "BloodPotency",
            trait?.Name ?? "Potencia de Sangue", from, specialization.Length == 0 ? from + 1 : from,
            cost, specialization, details.Clan, LoresheetRules.CampaignCost(details, cost));
        return new(details with
        {
            BloodPotency = trait is null ? from + 1 : details.BloodPotency,
            ExperienceSpent = details.ExperienceSpent + cost,
            CampaignExperienceSpent = details.CampaignExperienceSpent + purchase.CampaignCost,
            Advancement = state with { Purchases = [.. state.Purchases, purchase] }
        }, values);
    }

    public static string? ValidateHistory(CharacterDetails details, IReadOnlyList<TraitValue> traits)
    {
        var state = details.Advancement;
        if (state is null) return null;
        if (!Enum.IsDefined(state.Age) || !Enum.IsDefined(state.Profile) ||
            state.Purchases is null || state.Baseline is null || state.Purchases.Length > 2000 ||
            state.Purchases.Any(x => x is null || x.Specialization is null || x.Clan is null) ||
            state.Baseline.Any(x => x is null))
            return "Dados de criacao/evolucao invalidos.";
        if (state.AutomaticExperience)
        {
            if (traits.Count > 200 || traits.Any(x => x.Rating is < 0 or > 10 || x.Specialization is null) ||
                details.CampaignExperienceTotal < 0 ||
                details.CampaignExperienceTotal > 1000000 - StartingXp(state.Age) || details.BloodPotency is < 0 or > 10)
                return "Pontos ou fontes de XP invalidos.";
            var calculated = AutomaticExperience.Calculate(details, traits);
            return details.ExperienceTotal == calculated.Total && details.ExperienceSpent == calculated.Spent &&
                details.CampaignExperienceSpent == calculated.CampaignSpent ? null : "Os valores de XP nao correspondem aos pontos da ficha.";
        }
        if (state.IsCreating)
            return state.Purchases.Length == 0 && details.ExperienceSpent == LoresheetRules.InitialXpCost(traits) &&
                details.CampaignExperienceSpent == 0 ? null : "Na criacao, gastos de XP devem corresponder somente aos loresheets pagos com XP inicial.";
        if (details.PredatorSelection != state.BaselinePredatorSelection)
            return "O predador aplicado nao pode ser alterado depois da criacao.";
        if (CharacterRules.Validate(details with
            {
                Advancement = null, ExperienceSpent = state.BaselineSpent,
                CampaignExperienceSpent = 0, CampaignExperienceTotal = 0,
                ExperienceTotal = 1000000, BloodPotency = state.BaselineBloodPotency,
                NpcPhysical = state.BaselineNpcPhysical, NpcSocial = state.BaselineNpcSocial, NpcMental = state.BaselineNpcMental,
                HealthSuperficial = 0, HealthAggravated = 0, WillpowerSuperficial = 0, WillpowerAggravated = 0
            }, state.Baseline).Count != 0) return "A base inicial do historico e invalida.";
        var replay = details with
        {
            BloodPotency = state.BaselineBloodPotency, ExperienceSpent = state.BaselineSpent,
            CampaignExperienceSpent = state.BaselineCampaignSpent, CampaignExperienceTotal = 1000000,
            NpcPhysical = state.BaselineNpcPhysical, NpcSocial = state.BaselineNpcSocial, NpcMental = state.BaselineNpcMental,
            ExperienceTotal = 2000000, Advancement = state with { Purchases = [] }
        };
        IReadOnlyList<TraitValue> values = state.Baseline;
        try
        {
            foreach (var entry in state.Purchases)
            {
                // Reconstruct the funding available at purchase time; later XP awards must not change the recorded source.
                if (entry.CampaignCost < 0 || entry.CampaignCost > entry.Cost || entry.Cost < 0 ||
                    entry.Category == TraitCategory.Loresheet && entry.Loresheet is null)
                    return "Fonte de XP do historico invalida.";
                var purchaseDetails = replay with { Clan = entry.Clan,
                    ExperienceTotal = replay.ExperienceSpent + LoresheetRules.CampaignAvailable(replay) + entry.Cost - entry.CampaignCost };
                if (entry.Category == TraitCategory.Loresheet && state.HasCreationProfile)
                    purchaseDetails = purchaseDetails with
                    {
                        ExperienceTotal = Math.Max(purchaseDetails.ExperienceTotal, replay.CampaignExperienceTotal + StartingXp(state.Age))
                    };
                var result = entry.Category == TraitCategory.Loresheet
                    ? LoresheetRules.Select(purchaseDetails, values,
                        new(TraitCategory.Loresheet, entry.Key, entry.Name, entry.To, Loresheet: entry.Loresheet), LoresheetPayment.CampaignXp)
                    : Buy(purchaseDetails, values, entry.Category, entry.Key, entry.Specialization, entry.Name);
                var expected = result.Details.Advancement!.Purchases[^1];
                if (expected.From != entry.From || expected.To != entry.To || expected.Cost != entry.Cost ||
                    expected.Name != entry.Name || expected.CampaignCost != entry.CampaignCost || expected.Loresheet != entry.Loresheet)
                    return "Historico de XP inconsistente.";
                replay = result.Details;
                values = result.Traits;
            }
        }
        catch (InvalidOperationException exception) { return exception.Message; }
        if (replay.ExperienceSpent != details.ExperienceSpent || replay.CampaignExperienceSpent != details.CampaignExperienceSpent ||
            replay.BloodPotency != details.BloodPotency ||
            replay.NpcPhysical != details.NpcPhysical || replay.NpcSocial != details.NpcSocial || replay.NpcMental != details.NpcMental)
            return "XP gasto e Potencia de Sangue devem acompanhar as compras registradas.";
        bool Tracked(TraitValue x) => x.Category != TraitCategory.DisciplinePower &&
            (x.Rating > 0 || !string.IsNullOrEmpty(x.Specialization));
        var expectedValues = values.Where(Tracked).OrderBy(x => x.Category).ThenBy(x => x.Key).ToArray();
        var actual = traits.Where(Tracked).OrderBy(x => x.Category).ThenBy(x => x.Key).ToArray();
        return expectedValues.SequenceEqual(actual) ? null : "Use Evolucao para alterar pontos ou especializacoes apos concluir a criacao.";
    }
}
