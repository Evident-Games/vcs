using VCS.Domain.Advancement;
using VCS.Domain.Advantages;
using VCS.Domain.Predators;

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

    public const int NpcMaximumBoxes = 100;

    public static int HealthCapacity(IEnumerable<TraitValue> traits) => Attribute(traits, "Stamina") + 3;
    public static int WillpowerCapacity(IEnumerable<TraitValue> traits) =>
        Attribute(traits, "Composure") + Attribute(traits, "Resolve");

    private static int Attribute(IEnumerable<TraitValue> traits, string key) =>
        traits.Single(x => x.Category == TraitCategory.Attribute && x.Key == key).Rating;

    public static IReadOnlyList<ValidationIssue> Validate(CharacterDetails details, IReadOnlyList<TraitValue> traits)
    {
        var issues = new Issues();
        issues.AddRange(LoresheetRules.Validate(details, traits));
        issues.AddRange(PredatorRules.Validate(details, traits));
        if (CharacterAdvancement.ValidateHistory(details, traits) is { } advancementIssue)
            issues.Add(new(nameof(CharacterDetails.Advancement), advancementIssue));
        ValidateTexts(details, issues);
        ValidateExperience(details, issues);
        ValidateState(details, issues);
        ValidateNpc(details, issues);
        ValidateTraits(traits, issues);
        ValidateAttributesAndDamage(details, traits, issues);
        return issues;
    }

    /// <summary>Length limits of the identity and background texts.</summary>
    private static void ValidateTexts(CharacterDetails details, Issues issues)
    {
        issues.Text(nameof(details.Name), details.Name, 120, true);
        issues.Text(nameof(details.MortalName), details.MortalName, 120);
        issues.Text(nameof(details.Clan), details.Clan, 80);
        issues.Text(nameof(details.Sire), details.Sire, 120);
        issues.Text(nameof(details.Predator), details.Predator, 80);
        issues.Text(nameof(details.Concept), details.Concept, 200);
        issues.Text(nameof(details.Chronicle), details.Chronicle, 120);
        issues.Text(nameof(details.Description), details.Description, 20000);
        issues.Text(nameof(details.Notes), details.Notes, 100000);
        if (details.NoteEntries is null || details.NoteEntries.Count > 500)
            issues.Add(new(nameof(details.NoteEntries), "Uma ficha pode ter no maximo 500 notas."));
        foreach (var note in details.NoteEntries ?? [])
        {
            if (note is null) { issues.Add(new(nameof(details.NoteEntries), "Nota invalida.")); continue; }
            issues.Text("NoteEntries.Title", note.Title, 120);
            issues.Text("NoteEntries.Text", note.Text, 100000);
        }
        foreach (var (field, value) in new (string, string)[]
        {
            (nameof(details.Ambition), details.Ambition), (nameof(details.Desire), details.Desire),
            (nameof(details.Resonance), details.Resonance), (nameof(details.TrueAge), details.TrueAge),
            (nameof(details.ApparentAge), details.ApparentAge), (nameof(details.BirthDate), details.BirthDate),
            (nameof(details.DeathDate), details.DeathDate), (nameof(details.BloodSurge), details.BloodSurge),
            (nameof(details.DamageMend), details.DamageMend), (nameof(details.PowerBonus), details.PowerBonus),
            (nameof(details.RouseReroll), details.RouseReroll), (nameof(details.FeedingPenalty), details.FeedingPenalty),
            (nameof(details.BaneSeverity), details.BaneSeverity)
        }) issues.Text(field, value, 500);
        foreach (var (field, value) in new (string, string)[]
        {
            (nameof(details.ChronicleTenets), details.ChronicleTenets), (nameof(details.Touchstones), details.Touchstones),
            (nameof(details.Convictions), details.Convictions), (nameof(details.ClanBane), details.ClanBane),
            (nameof(details.Appearance), details.Appearance), (nameof(details.DistinguishingFeatures), details.DistinguishingFeatures),
            (nameof(details.History), details.History)
        }) issues.Text(field, value, 20000);
    }

    /// <summary>Experience ranges and consistency between total, spent and campaign XP.</summary>
    private static void ValidateExperience(CharacterDetails details, Issues issues)
    {
        issues.Range(nameof(details.ExperienceTotal), details.ExperienceTotal, 0, 1000000);
        issues.Range(nameof(details.ExperienceSpent), details.ExperienceSpent, 0, 1000000);
        issues.Range(nameof(details.CampaignExperienceTotal), details.CampaignExperienceTotal, 0, 1000000);
        issues.Range(nameof(details.CampaignExperienceSpent), details.CampaignExperienceSpent, 0, 1000000);
        if (details.Advancement?.AutomaticExperience != true && (details.CampaignExperienceTotal > details.ExperienceTotal ||
            details.CampaignExperienceSpent > details.CampaignExperienceTotal ||
            details.CampaignExperienceSpent > details.ExperienceSpent ||
            (long)details.ExperienceTotal - details.ExperienceSpent < LoresheetRules.CampaignAvailable(details)))
            issues.Add(new(nameof(CharacterDetails.CampaignExperienceTotal), "As fontes de XP nao conferem: campanha faz parte do total e seus gastos nao podem exceder o recebido."));
        if (details.Advancement is { HasCreationProfile: true } profile && Enum.IsDefined(profile.Age) &&
            details.CampaignExperienceTotal > details.ExperienceTotal - CharacterAdvancement.StartingXp(profile.Age))
            issues.Add(new(nameof(CharacterDetails.CampaignExperienceTotal), "O bonus inicial de Neofito/Ancillae nao pode ser classificado como XP de campanha."));
        if (details.Advancement?.AutomaticExperience != true && details.ExperienceSpent > details.ExperienceTotal)
            issues.Add(new(nameof(details.ExperienceSpent), "A experiencia gasta nao pode exceder a experiencia total."));
    }

    /// <summary>Generation, hunger, humanity, stains and blood potency ranges.</summary>
    private static void ValidateState(CharacterDetails details, Issues issues)
    {
        issues.Range(nameof(details.Generation), details.Generation, 1, 16);
        issues.Range(nameof(details.Hunger), details.Hunger, 0, 5);
        issues.Range(nameof(details.Humanity), details.Humanity, 0, 10);
        issues.Range(nameof(details.Stains), details.Stains, 0, 1000000);
        issues.Range(nameof(details.BloodPotency), details.BloodPotency, 0, 10);
    }

    /// <summary>NPC quick sheet: attribute pools, box totals and attacks.</summary>
    private static void ValidateNpc(CharacterDetails details, Issues issues)
    {
        issues.Range(nameof(details.NpcPhysical), details.NpcPhysical, 0, 5);
        issues.Range(nameof(details.NpcSocial), details.NpcSocial, 0, 5);
        issues.Range(nameof(details.NpcMental), details.NpcMental, 0, 5);
        issues.Range(nameof(details.NpcHealthCapacity), details.NpcHealthCapacity, 1, NpcMaximumBoxes);
        issues.Range(nameof(details.NpcWillpowerCapacity), details.NpcWillpowerCapacity, 1, NpcMaximumBoxes);
        if (details.NpcAttacks is null || details.NpcAttacks.Count > 50)
            issues.Add(new(nameof(details.NpcAttacks), "Um NPC pode ter no maximo 50 ataques."));
        foreach (var attack in details.NpcAttacks ?? [])
        {
            if (attack is null) { issues.Add(new(nameof(details.NpcAttacks), "Ataque de NPC invalido.")); continue; }
            issues.Text("NpcAttacks.Name", attack.Name, 200);
            issues.Text("NpcAttacks.DicePool", attack.DicePool, 200);
            issues.Text("NpcAttacks.DamageBonus", attack.DamageBonus, 200);
            issues.Text("NpcAttacks.DamageType", attack.DamageType, 200);
            issues.Text("NpcAttacks.ActiveDisciplines", attack.ActiveDisciplines, 500);
            issues.Text("NpcAttacks.Notes", attack.Notes, 2000);
        }
        if (details.IsNpc && details.WillpowerSuperficial != 0)
            issues.Add(new("Willpower", "A Forca de Vontade de NPC usa somente caixas com X."));
    }

    /// <summary>Trait count, text limits, details matching the category, ratings and duplicate keys.</summary>
    private static void ValidateTraits(IReadOnlyList<TraitValue> traits, Issues issues)
    {
        if (traits.Count > 200)
            issues.Add(new(nameof(Character.Traits), "Uma ficha pode ter no maximo 200 tracos."));
        foreach (var trait in traits)
        {
            issues.Text("Trait.Key", trait.Key, 80, true);
            issues.Text("Trait.Name", trait.Name, 80, true);
            issues.Text("Trait.Specialization", trait.Specialization, 500);
            if (trait.Merit is { } merit)
            {
                issues.Text("Trait.Merit.Group", merit.Group, 80, true);
                issues.Text("Trait.Merit.Description", merit.Description, 20000);
                if (trait.Category is not (TraitCategory.Advantage or TraitCategory.Flaw))
                    issues.Add(new(nameof(Character.Traits), "Familia e descricao pessoal devem pertencer a vantagens ou defeitos."));
            }
            if (trait.Skill is { } skill)
            {
                issues.Text("Trait.Skill.Group", skill.Group, 80, true);
                if (trait.Category != TraitCategory.Skill)
                    issues.Add(new(nameof(Character.Traits), "O grupo pessoal deve pertencer a uma habilidade."));
            }
            if (trait.Power is { } power)
            {
                issues.Text("Trait.Power.Discipline", power.Discipline, 80);
                issues.Text("Trait.Power.Cost", power.Cost, 200);
                issues.Text("Trait.Power.DicePool", power.DicePool, 200);
                issues.Text("Trait.Power.Duration", power.Duration, 200);
                issues.Text("Trait.Power.Prerequisite", power.Prerequisite, 200);
                foreach (var (field, text) in new[] { ("Description", power.Description), ("Ingredients", power.Ingredients),
                             ("Process", power.Process), ("System", power.System) })
                    issues.Text($"Trait.Power.{field}", text, 20000);
                if (trait.Category != TraitCategory.DisciplinePower)
                    issues.Add(new(nameof(Character.Traits), "Detalhes de poder devem pertencer a poderes ou rituais."));
            }
            if (trait.Category != TraitCategory.Skill && !string.IsNullOrEmpty(trait.Specialization))
                issues.Add(new(nameof(Character.Traits), "Especializacoes devem pertencer a habilidades."));
            if (!Enum.IsDefined(trait.Category))
                issues.Add(new(nameof(Character.Traits), "Categoria de traco desconhecida."));
            issues.Range(trait.Name, trait.Rating,
                trait.Category is TraitCategory.DisciplinePower ? 1 : 0, 5);
        }
        if (traits.GroupBy(x => (x.Category, x.Key?.ToUpperInvariant())).Any(x => x.Count() > 1))
            issues.Add(new(nameof(Character.Traits), "Existem identificadores de traco duplicados."));
    }

    /// <summary>The nine basic attributes, then damage within health and willpower capacity.</summary>
    private static void ValidateAttributesAndDamage(CharacterDetails details, IReadOnlyList<TraitValue> traits, Issues issues)
    {
        var attributes = traits.Where(x => x.Category == TraitCategory.Attribute).ToList();
        var validAttributes = attributes.Count == DefaultAttributes.Count &&
            DefaultAttributes.All(x => attributes.Count(a => a.Key == x.Key) == 1);
        if (!validAttributes)
            issues.Add(new(SheetSection.Attributes, "A ficha deve conter exatamente os nove atributos basicos."));
        else
        {
            issues.Damage("Health", details.HealthSuperficial, details.HealthAggravated,
                details.IsNpc ? details.NpcHealthCapacity : HealthCapacity(traits));
            issues.Damage("Willpower", details.WillpowerSuperficial, details.WillpowerAggravated,
                details.IsNpc ? details.NpcWillpowerCapacity : WillpowerCapacity(traits));
        }
    }

    /// <summary>Collects issues; field labels come from <see cref="Label"/>.</summary>
    private sealed class Issues : List<ValidationIssue>
    {
        public void Text(string field, string? value, int max, bool required = false)
        {
            if (value is null || (required && string.IsNullOrWhiteSpace(value)) || value.Length > max)
                Add(new(field, $"{Label(field)}: informe {(required ? "um valor de 1 a" : "ate")} {max} caracteres."));
        }

        public void Range(string field, int value, int min, int max)
        {
            if (value < min || value > max)
                Add(new(field, $"{Label(field)}: o valor deve estar entre {min} e {max}."));
        }

        public void Damage(string field, int superficial, int aggravated, int capacity)
        {
            if (superficial < 0 || aggravated < 0 || (long)superficial + aggravated > capacity)
                Add(new(field, $"{Label(field)}: os danos devem ser positivos ou zero e a soma nao pode exceder {capacity}."));
        }
    }

    private static string Label(string field) => field switch
    {
        nameof(CharacterDetails.NpcPhysical) => "Atributos fisicos do NPC",
        nameof(CharacterDetails.NpcSocial) => "Atributos sociais do NPC",
        nameof(CharacterDetails.NpcMental) => "Atributos mentais do NPC",
        nameof(CharacterDetails.NpcHealthCapacity) => "Total de Vitalidade do NPC",
        nameof(CharacterDetails.NpcWillpowerCapacity) => "Total de Forca de Vontade do NPC",
        "NpcAttacks.Name" => "Ataque do NPC",
        "NpcAttacks.DicePool" => "Parada de dados do ataque",
        "NpcAttacks.DamageBonus" => "Bonus de dano do ataque",
        "NpcAttacks.DamageType" => "Tipo de dano do ataque",
        "NpcAttacks.ActiveDisciplines" => "Disciplinas ativas do ataque",
        "NpcAttacks.Notes" => "Notas do ataque",
        nameof(CharacterDetails.Name) => "Nome",
        nameof(CharacterDetails.MortalName) => "Nome mortal",
        nameof(CharacterDetails.Clan) => "Cla",
        nameof(CharacterDetails.Generation) => "Geracao",
        nameof(CharacterDetails.Predator) => "Predador",
        nameof(CharacterDetails.Concept) => "Conceito",
        nameof(CharacterDetails.Chronicle) => "Cronica",
        nameof(CharacterDetails.Description) => "Descricao",
        nameof(CharacterDetails.Notes) => "Anotacoes",
        "NoteEntries.Title" => "Titulo da nota",
        "NoteEntries.Text" => "Texto da nota",
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
        nameof(CharacterDetails.Stains) => "Maculas",
        nameof(CharacterDetails.BloodPotency) => "Potencia de Sangue",
        "Trait.Name" => "Nome do traco",
        "Trait.Key" => "Identificador do traco",
        "Health" => "Saude",
        "Willpower" => "Forca de Vontade",
        _ => field
    };
}
