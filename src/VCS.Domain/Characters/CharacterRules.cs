namespace VCS.Domain.Characters;

public sealed record ValidationIssue(string Field, string Message);

public sealed class CharacterValidationException(IReadOnlyList<ValidationIssue> issues)
    : Exception(string.Join(Environment.NewLine, issues.Select(x => x.Message)))
{
    public IReadOnlyList<ValidationIssue> Issues { get; } = issues;
}

public static class CharacterRules
{
    public static IReadOnlyList<TraitValue> DefaultAttributes { get; } = Array.AsReadOnly(new[]
    {
        new TraitValue(TraitCategory.Attribute, "Strength", "Forca", 1),
        new TraitValue(TraitCategory.Attribute, "Dexterity", "Destreza", 1),
        new TraitValue(TraitCategory.Attribute, "Stamina", "Vigor", 1),
        new TraitValue(TraitCategory.Attribute, "Charisma", "Carisma", 1),
        new TraitValue(TraitCategory.Attribute, "Manipulation", "Manipulacao", 1),
        new TraitValue(TraitCategory.Attribute, "Composure", "Autocontrole", 1),
        new TraitValue(TraitCategory.Attribute, "Intelligence", "Inteligencia", 1),
        new TraitValue(TraitCategory.Attribute, "Wits", "Raciocinio", 1),
        new TraitValue(TraitCategory.Attribute, "Resolve", "Determinacao", 1)
    });

    public static int HealthCapacity(IEnumerable<TraitValue> traits) => Attribute(traits, "Stamina") + 3;
    public static int WillpowerCapacity(IEnumerable<TraitValue> traits) =>
        Attribute(traits, "Composure") + Attribute(traits, "Resolve");

    private static int Attribute(IEnumerable<TraitValue> traits, string key) =>
        traits.Single(x => x.Category == TraitCategory.Attribute && x.Key == key).Rating;

    public static IReadOnlyList<ValidationIssue> Validate(CharacterDetails details, IReadOnlyList<TraitValue> traits)
    {
        var issues = new List<ValidationIssue>();
        issues.AddRange(LoresheetRules.Validate(details, traits));
        if (CharacterAdvancement.ValidateHistory(details, traits) is { } advancementIssue)
            issues.Add(new("Advancement", advancementIssue));
        void Text(string field, string? value, int max, bool required = false)
        {
            if (value is null || (required && string.IsNullOrWhiteSpace(value)) || value.Length > max)
                issues.Add(new(field, $"{Label(field)}: informe {(required ? "um valor de 1 a" : "ate")} {max} caracteres."));
        }
        void Range(string field, int value, int min, int max)
        {
            if (value < min || value > max)
                issues.Add(new(field, $"{Label(field)}: o valor deve estar entre {min} e {max}."));
        }

        Text(nameof(details.Name), details.Name, 120, true);
        Text(nameof(details.MortalName), details.MortalName, 120);
        Text(nameof(details.Clan), details.Clan, 80);
        Text(nameof(details.Sire), details.Sire, 120);
        Text(nameof(details.Predator), details.Predator, 80);
        Text(nameof(details.Concept), details.Concept, 200);
        Text(nameof(details.Chronicle), details.Chronicle, 120);
        Text(nameof(details.Description), details.Description, 20000);
        Text(nameof(details.Notes), details.Notes, 100000);
        foreach (var (field, value) in new (string, string)[]
        {
            (nameof(details.Ambition), details.Ambition), (nameof(details.Desire), details.Desire),
            (nameof(details.Resonance), details.Resonance), (nameof(details.TrueAge), details.TrueAge),
            (nameof(details.ApparentAge), details.ApparentAge), (nameof(details.BirthDate), details.BirthDate),
            (nameof(details.DeathDate), details.DeathDate), (nameof(details.BloodSurge), details.BloodSurge),
            (nameof(details.DamageMend), details.DamageMend), (nameof(details.PowerBonus), details.PowerBonus),
            (nameof(details.RouseReroll), details.RouseReroll), (nameof(details.FeedingPenalty), details.FeedingPenalty),
            (nameof(details.BaneSeverity), details.BaneSeverity)
        }) Text(field, value, 500);
        foreach (var (field, value) in new (string, string)[]
        {
            (nameof(details.ChronicleTenets), details.ChronicleTenets), (nameof(details.Touchstones), details.Touchstones),
            (nameof(details.Convictions), details.Convictions), (nameof(details.ClanBane), details.ClanBane),
            (nameof(details.Appearance), details.Appearance), (nameof(details.DistinguishingFeatures), details.DistinguishingFeatures),
            (nameof(details.History), details.History)
        }) Text(field, value, 20000);
        Range(nameof(details.ExperienceTotal), details.ExperienceTotal, 0, 1000000);
        Range(nameof(details.ExperienceSpent), details.ExperienceSpent, 0, 1000000);
        Range(nameof(details.CampaignExperienceTotal), details.CampaignExperienceTotal, 0, 1000000);
        Range(nameof(details.CampaignExperienceSpent), details.CampaignExperienceSpent, 0, 1000000);
        if (details.CampaignExperienceTotal > details.ExperienceTotal ||
            details.CampaignExperienceSpent > details.CampaignExperienceTotal ||
            details.CampaignExperienceSpent > details.ExperienceSpent ||
            (long)details.ExperienceTotal - details.ExperienceSpent < LoresheetRules.CampaignAvailable(details))
            issues.Add(new("CampaignExperienceTotal", "As fontes de XP nao conferem: campanha faz parte do total e seus gastos nao podem exceder o recebido."));
        if (details.Advancement is { HasCreationProfile: true } profile && Enum.IsDefined(profile.Age) &&
            details.CampaignExperienceTotal > details.ExperienceTotal - CharacterAdvancement.StartingXp(profile.Age))
            issues.Add(new("CampaignExperienceTotal", "O bonus inicial de Neofito/Ancillae nao pode ser classificado como XP de campanha."));
        if (details.ExperienceSpent > details.ExperienceTotal)
            issues.Add(new(nameof(details.ExperienceSpent), "A experiencia gasta nao pode exceder a experiencia total."));
        Range(nameof(details.Generation), details.Generation, 1, 16);
        Range(nameof(details.Hunger), details.Hunger, 0, 5);
        Range(nameof(details.Humanity), details.Humanity, 0, 10);
        Range(nameof(details.BloodPotency), details.BloodPotency, 0, 10);
        Range(nameof(details.NpcPhysical), details.NpcPhysical, 0, 5);
        Range(nameof(details.NpcSocial), details.NpcSocial, 0, 5);
        Range(nameof(details.NpcMental), details.NpcMental, 0, 5);
        Range(nameof(details.NpcHealthCapacity), details.NpcHealthCapacity, 1, 20);
        Range(nameof(details.NpcWillpowerCapacity), details.NpcWillpowerCapacity, 1, 20);
        if (details.IsNpc && details.WillpowerSuperficial != 0)
            issues.Add(new("Willpower", "A Forca de Vontade de NPC usa somente caixas com X."));

        if (traits.Count > 200)
            issues.Add(new("Traits", "Uma ficha pode ter no maximo 200 tracos."));
        foreach (var trait in traits)
        {
            Text("Trait.Key", trait.Key, 80, true);
            Text("Trait.Name", trait.Name, 80, true);
            Text("Trait.Specialization", trait.Specialization, 500);
            if (trait.Category != TraitCategory.Skill && !string.IsNullOrEmpty(trait.Specialization))
                issues.Add(new("Traits", "Especializacoes devem pertencer a habilidades."));
            if (!Enum.IsDefined(trait.Category))
                issues.Add(new("Traits", "Categoria de traco desconhecida."));
            Range(trait.Name, trait.Rating,
                trait.Category is TraitCategory.Attribute or TraitCategory.DisciplinePower ? 1 : 0, 5);
        }
        if (traits.GroupBy(x => (x.Category, x.Key?.ToUpperInvariant())).Any(x => x.Count() > 1))
            issues.Add(new("Traits", "Existem identificadores de traco duplicados."));

        var attributes = traits.Where(x => x.Category == TraitCategory.Attribute).ToList();
        var validAttributes = attributes.Count == DefaultAttributes.Count &&
            DefaultAttributes.All(x => attributes.Count(a => a.Key == x.Key) == 1);
        if (!validAttributes)
            issues.Add(new("Attributes", "A ficha deve conter exatamente os nove atributos basicos."));
        else
        {
            Damage("Health", details.HealthSuperficial, details.HealthAggravated,
                details.IsNpc ? details.NpcHealthCapacity : HealthCapacity(traits));
            Damage("Willpower", details.WillpowerSuperficial, details.WillpowerAggravated,
                details.IsNpc ? details.NpcWillpowerCapacity : WillpowerCapacity(traits));
        }
        return issues;

        void Damage(string field, int superficial, int aggravated, int capacity)
        {
            if (superficial < 0 || aggravated < 0 || (long)superficial + aggravated > capacity)
                issues.Add(new(field, $"{Label(field)}: os danos devem ser positivos ou zero e a soma nao pode exceder {capacity}."));
        }
    }

    private static string Label(string field) => field switch
    {
        nameof(CharacterDetails.NpcPhysical) => "Atributos fisicos do NPC",
        nameof(CharacterDetails.NpcSocial) => "Atributos sociais do NPC",
        nameof(CharacterDetails.NpcMental) => "Atributos mentais do NPC",
        nameof(CharacterDetails.NpcHealthCapacity) => "Total de Vitalidade do NPC",
        nameof(CharacterDetails.NpcWillpowerCapacity) => "Total de Forca de Vontade do NPC",
        nameof(CharacterDetails.Name) => "Nome",
        nameof(CharacterDetails.MortalName) => "Nome mortal",
        nameof(CharacterDetails.Clan) => "Cla",
        nameof(CharacterDetails.Generation) => "Geracao",
        nameof(CharacterDetails.Predator) => "Predador",
        nameof(CharacterDetails.Concept) => "Conceito",
        nameof(CharacterDetails.Chronicle) => "Cronica",
        nameof(CharacterDetails.Description) => "Descricao",
        nameof(CharacterDetails.Notes) => "Anotacoes",
        nameof(CharacterDetails.Ambition) => "Ambicao",
        nameof(CharacterDetails.Desire) => "Desejo",
        nameof(CharacterDetails.Resonance) => "Ressonancia",
        nameof(CharacterDetails.ChronicleTenets) => "Principios da cronica",
        nameof(CharacterDetails.Touchstones) => "Pilares",
        nameof(CharacterDetails.Convictions) => "Conviccoes",
        nameof(CharacterDetails.ClanBane) => "Perdicao do cla",
        nameof(CharacterDetails.TrueAge) => "Idade real",
        nameof(CharacterDetails.ApparentAge) => "Idade aparente",
        nameof(CharacterDetails.BirthDate) => "Nascimento",
        nameof(CharacterDetails.DeathDate) => "Morte",
        nameof(CharacterDetails.Appearance) => "Aparencia",
        nameof(CharacterDetails.DistinguishingFeatures) => "Marcas distintivas",
        nameof(CharacterDetails.History) => "Historia",
        nameof(CharacterDetails.BloodSurge) => "Surto de Sangue",
        nameof(CharacterDetails.DamageMend) => "Recuperacao de dano",
        nameof(CharacterDetails.PowerBonus) => "Bonus de poderes",
        nameof(CharacterDetails.RouseReroll) => "Rerrolagem de Sangue",
        nameof(CharacterDetails.FeedingPenalty) => "Penalidade de alimentacao",
        nameof(CharacterDetails.BaneSeverity) => "Severidade da perdicao",
        nameof(CharacterDetails.ExperienceTotal) => "Experiencia total",
        nameof(CharacterDetails.ExperienceSpent) => "Experiencia gasta",
        nameof(CharacterDetails.Hunger) => "Fome",
        nameof(CharacterDetails.Humanity) => "Humanidade",
        nameof(CharacterDetails.BloodPotency) => "Potencia de Sangue",
        "Trait.Name" => "Nome do traco",
        "Trait.Key" => "Identificador do traco",
        "Health" => "Saude",
        "Willpower" => "Forca de Vontade",
        _ => field
    };
}
