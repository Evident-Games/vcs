namespace VCS.Domain.Characters;

public static class LoresheetRules
{
    public static int InitialXpCost(IEnumerable<TraitValue> traits) =>
        traits.Where(x => x.Category == TraitCategory.Loresheet && x.Loresheet?.Payment == LoresheetPayment.InitialXp)
            .Sum(x => x.Rating * 3);

    public static int AdvantagePoints(IEnumerable<TraitValue> traits) =>
        traits.Where(x => x.Category == TraitCategory.Loresheet && x.Loresheet?.Payment == LoresheetPayment.AdvantagePoints)
            .Sum(x => x.Rating);

    public static int CampaignAvailable(CharacterDetails details) => details.CampaignExperienceTotal - details.CampaignExperienceSpent;

    public static int CampaignCost(CharacterDetails details, int cost) =>
        Math.Max(0, cost - (details.ExperienceTotal - details.ExperienceSpent - CampaignAvailable(details)));

    public static AdvancementResult Select(CharacterDetails details, IReadOnlyList<TraitValue> traits,
        TraitValue benefit, LoresheetPayment payment)
    {
        if (benefit.Category != TraitCategory.Loresheet || benefit.Loresheet is null ||
            benefit.Rating is < 1 or > 5 || !Enum.IsDefined(payment))
            throw new InvalidOperationException("Beneficio de loresheet invalido.");
        if (traits.Any(x => x.Category == TraitCategory.Loresheet && x.Key == benefit.Key))
            throw new InvalidOperationException("Este beneficio ja esta na ficha.");
        var state = details.Advancement ?? throw new InvalidOperationException("Inicie a criacao ou registre a base atual para comprar loresheets.");
        if (!Enum.IsDefined(state.Age)) throw new InvalidOperationException("Idade vampirica invalida.");
        if (state.HasCreationProfile && Enum.IsDefined(state.Age) &&
            details.CampaignExperienceTotal > details.ExperienceTotal - CharacterAdvancement.StartingXp(state.Age))
            throw new InvalidOperationException("O bonus inicial de idade nao pode ser reclassificado como XP de campanha.");
        var selection = benefit with { Loresheet = benefit.Loresheet with { Payment = payment } };
        if (state.IsCreating)
        {
            if (payment == LoresheetPayment.CampaignXp)
                throw new InvalidOperationException("Na criacao, escolha pontos de vantagens ou XP inicial.");
            if (traits.Any(x => x.Category == TraitCategory.Loresheet && x.Loresheet?.IsLineage == benefit.Loresheet.IsLineage))
                throw new InvalidOperationException(benefit.Loresheet.IsLineage
                    ? "A criacao permite somente um beneficio de linhagem/descendencia."
                    : "A criacao permite somente um beneficio geral, mesmo de outra colecao.");
            var values = traits.Append(selection).ToArray();
            if (payment == LoresheetPayment.AdvantagePoints &&
                traits.Where(x => x.Category == TraitCategory.Advantage).Sum(x => x.Rating) + AdvantagePoints(values) >
                CharacterAdvancement.AdvantageBudget(state.Age))
                throw new InvalidOperationException("Pontos iniciais de vantagens insuficientes. Remova pontos de outra vantagem ou escolha XP inicial.");
            var initialCost = InitialXpCost(values);
            if (initialCost > CharacterAdvancement.StartingXp(state.Age) || initialCost > details.ExperienceTotal - details.CampaignExperienceTotal)
                throw new InvalidOperationException("XP inicial de idade insuficiente para este beneficio.");
            return new(details with { ExperienceSpent = initialCost }, values);
        }
        if (payment != LoresheetPayment.CampaignXp)
            throw new InvalidOperationException("Apos a criacao, loresheets usam exclusivamente XP de campanha.");
        if (state.Purchases.Length >= 2000) throw new InvalidOperationException("Limite de 2000 compras atingido.");
        var cost = benefit.Rating * 3;
        if (CampaignAvailable(details) < cost || details.ExperienceTotal - details.ExperienceSpent < cost)
            throw new InvalidOperationException($"XP de campanha insuficiente: este beneficio custa {cost} XP. O XP inicial de Neofito/Ancillae nao pode ser usado.");
        return new(details with
        {
            ExperienceSpent = details.ExperienceSpent + cost,
            CampaignExperienceSpent = details.CampaignExperienceSpent + cost,
            Advancement = state with
            {
                Purchases = [.. state.Purchases, new(DateTime.UtcNow, TraitCategory.Loresheet, benefit.Key,
                    benefit.Name, 0, benefit.Rating, cost, CampaignCost: cost, Loresheet: selection.Loresheet)]
            }
        }, traits.Append(selection).ToArray());
    }

    public static AdvancementResult RemoveInitial(CharacterDetails details, IReadOnlyList<TraitValue> traits, string key)
    {
        if (details.Advancement?.IsCreating != true)
            throw new InvalidOperationException("Somente beneficios da criacao em andamento podem ser removidos.");
        if (!traits.Any(x => x.Category == TraitCategory.Loresheet && x.Key == key))
            throw new InvalidOperationException("Beneficio nao encontrado na ficha.");
        var values = traits.Where(x => x.Category != TraitCategory.Loresheet || x.Key != key).ToArray();
        return new(details with { ExperienceSpent = InitialXpCost(values) }, values);
    }

    public static IReadOnlyList<ValidationIssue> Validate(CharacterDetails details, IReadOnlyList<TraitValue> traits)
    {
        var issues = new List<ValidationIssue>();
        foreach (var trait in traits)
        {
            if (trait.Category != TraitCategory.Loresheet)
            {
                if (trait.Loresheet is not null) issues.Add(new("Loresheets", "Metadados de loresheet pertencem somente a beneficios."));
                continue;
            }
            if (trait.Rating is < 1 or > 5 || trait.Loresheet is not { } lore ||
                string.IsNullOrWhiteSpace(lore.Collection) || lore.Collection.Length > 80 ||
                lore.Description is null || lore.Description.Length > 20000 || !Enum.IsDefined(lore.Payment))
                issues.Add(new("Loresheets", "Colecao, descricao, pontuacao ou pagamento do beneficio invalidos."));
        }
        if (details.Advancement?.IsCreating == true)
        {
            var selections = traits.Where(x => x.Category == TraitCategory.Loresheet && x.Loresheet is not null).ToArray();
            if (selections.GroupBy(x => x.Loresheet!.IsLineage).Any(x => x.Count() > 1))
                issues.Add(new("Loresheets", "Criacao: somente um beneficio geral e um de linhagem/descendencia."));
            if (selections.Any(x => x.Loresheet!.Payment == LoresheetPayment.CampaignXp))
                issues.Add(new("Loresheets", "Compras da criacao usam pontos de vantagens ou XP inicial, nao campanha."));
            if (Enum.IsDefined(details.Advancement.Age) && InitialXpCost(selections) > CharacterAdvancement.StartingXp(details.Advancement.Age))
                issues.Add(new("Loresheets", "O custo de loresheets excede o XP inicial de idade."));
        }
        return issues;
    }
}
