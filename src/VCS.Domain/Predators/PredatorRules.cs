using VCS.Domain.Advancement;
using VCS.Domain.Characters;
using VCS.Domain.Clans;

namespace VCS.Domain.Predators;

public sealed record PredatorChoice(string Id, string Discipline, int SpecializationIndex, string SpecializationText,
    int[] AdvantageAllocation, int[] FlawAllocation);

public static class PredatorRules
{
    public static bool IsGrant(TraitValue value) => value.Key?.StartsWith("predator:", StringComparison.Ordinal) == true;
    public static int SpecializationCount(CharacterDetails details, TraitValue skill)
    {
        var count = CharacterAdvancement.SpecializationCount(skill.Specialization);
        if (details.PredatorSelection is { } applied && skill.Key == applied.SkillKey &&
            skill.Specialization.Split([',', ';', '\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Any(x => ClanCatalog.SameName(x, applied.Specialization))) count--;
        return Math.Max(0, count);
    }

    public static int MaximumFor(string option) =>
        option is "Perdicao Folclorica" or "Tabu Folclorico" or "Estigma" ? 1 : option == "Isca de Estaca" ? 2 : 5;

    /// <summary>Variable predator points (advantages, flaws) still waiting to be placed.</summary>
    public static (int Advantages, int Flaws) Unallocated(PredatorSelection receipt)
    {
        if (PredatorCatalog.Find(receipt.Id) is not { } definition) return (0, 0);
        var chosen = receipt.Grants.Skip(definition.FixedGrants.Length).ToArray();
        return (Math.Max(0, definition.AllocationPoints - chosen.Where(x => x.Category == TraitCategory.Advantage).Sum(x => x.Rating)),
            Math.Max(0, definition.FlawPoints - chosen.Where(x => x.Category == TraitCategory.Flaw).Sum(x => x.Rating)));
    }

    public static AdvancementResult Apply(CharacterDetails details, IReadOnlyList<TraitValue> traits, PredatorChoice choice)
    {
        if (details.Advancement?.IsCreating != true) throw new InvalidOperationException("Predadores sao aplicados somente durante a criacao.");
        var original = Remove(details, traits, requireSelection: false);
        details = original.Details;
        traits = original.Traits;
        var definition = PredatorCatalog.Find(choice.Id) ?? throw new InvalidOperationException("Predador nao encontrado.");
        if (definition.Id is "bagger" or "farmer" && ClanCatalog.Find(details.Clan)?.Name == "Ventrue")
            throw new InvalidOperationException("Ventrue nao pode escolher este predador.");
        if (definition.Id == "farmer" && details.BloodPotency >= 3)
            throw new InvalidOperationException("Fazendeiro exige Potencia de Sangue menor que 3 na escolha.");
        // Discipline and specialization are optional so the sheet can apply each choice as soon as it is made.
        var hasDiscipline = !string.IsNullOrWhiteSpace(choice.Discipline);
        if (hasDiscipline && !PredatorCatalog.DisciplinesFor(definition, details.Clan).Any(x => ClanCatalog.SameName(x, choice.Discipline)))
            throw new InvalidOperationException("Disciplina invalida para este predador/cla. Feiticaria de Sangue e exclusiva de Tremere.");
        var (skill, specialization) = ChosenSpecialization(definition, traits, choice);
        var discipline = hasDiscipline
            ? traits.SingleOrDefault(x => x.Category == TraitCategory.Discipline && ClanCatalog.SameName(x.Name, choice.Discipline)) : null;
        if (discipline?.Rating >= 5) throw new InvalidOperationException("A disciplina ja esta no nivel maximo.");
        var grants = Grants(definition, choice);
        var humanity = details.Humanity + definition.HumanityDelta;
        var potency = details.BloodPotency + definition.BloodDelta;
        if (humanity is < 0 or > 10 || potency is < 0 or > 10)
            throw new InvalidOperationException("O bonus/custo excede os limites de Humanidade ou Potencia de Sangue. Ajuste a base antes de aplicar.");
        var added = skill is not null && !skill.Specialization.Split([',', ';', '\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(x => ClanCatalog.SameName(x, specialization));
        var text = skill is null || !added ? skill?.Specialization ?? "" : string.IsNullOrWhiteSpace(skill.Specialization)
            ? specialization : $"{skill.Specialization.TrimEnd()}\n{specialization}";
        if (text.Length > 500) throw new InvalidOperationException("Especializacoes excedem o limite de 500 caracteres.");
        var values = traits.Select(x => skill is not null && x == skill ? x with { Specialization = text }
            : discipline is not null && x == discipline ? x with { Rating = x.Rating + 1 } : x).ToList();
        var disciplineKey = !hasDiscipline ? "" : discipline?.Key ?? "pred-disc-" + definition.Id;
        if (hasDiscipline && discipline is null) values.Add(new(TraitCategory.Discipline, disciplineKey, choice.Discipline, 1));
        values.AddRange(grants);
        return new(details with
        {
            Predator = definition.Name, Humanity = humanity, BloodPotency = potency,
            PredatorSelection = new(definition.Id, disciplineKey, hasDiscipline ? choice.Discipline : "", skill?.Key ?? "", skill?.Name ?? "",
                specialization, added, grants.ToArray(), definition.HumanityDelta, definition.BloodDelta)
        }, values);
    }

    /// <summary>The skill and text of the chosen predator specialization; (null, "") when none is chosen yet.</summary>
    private static (TraitValue? Skill, string Specialization) ChosenSpecialization(PredatorDefinition definition,
        IReadOnlyList<TraitValue> traits, PredatorChoice choice)
    {
        if (choice.SpecializationIndex < -1 || choice.SpecializationIndex >= definition.Specializations.Length)
            throw new InvalidOperationException("Escolha uma especializacao do predador.");
        if (choice.SpecializationIndex < 0) return (null, "");
        var option = definition.Specializations[choice.SpecializationIndex];
        var skill = traits.SingleOrDefault(x => x.Category == TraitCategory.Skill && ClanCatalog.SameName(x.Name, option.Skill))
            ?? throw new InvalidOperationException("A habilidade escolhida nao esta na ficha. Carregue o catalogo de habilidades.");
        if (skill.Rating < 1) throw new InvalidOperationException($"Coloque pelo menos um ponto em {skill.Name} para receber a especializacao do predador.");
        var specialization = option.IsFreeText ? (choice.SpecializationText ?? "").Trim() : option.Text;
        if (string.IsNullOrWhiteSpace(specialization) || specialization.Length > 120 ||
            CharacterAdvancement.SpecializationCount(specialization) != 1)
            throw new InvalidOperationException("Informe uma especializacao de 1 a 120 caracteres, sem separadores.");
        return (skill, specialization);
    }

    /// <summary>Fixed grants plus the advantage and flaw points the player allocated, as keyed predator traits.</summary>
    private static List<TraitValue> Grants(PredatorDefinition definition, PredatorChoice choice)
    {
        var grants = new List<TraitValue>();
        void Grant(PredatorAllocation entry, int points)
        {
            if (points > 0) grants.Add(new(entry.Category, $"predator:{definition.Id}:{grants.Count}", entry.Name, points));
        }
        void Allocate(PredatorAllocation[] options, int[] allocation, int budget, bool chooseOne)
        {
            // Points may stay unallocated while the sheet is being built; CreationIssues reports what is left.
            if (allocation is null || allocation.Length != options.Length || allocation.Any(x => x < 0 || x > 5) ||
                allocation.Sum() > budget || chooseOne && allocation.Count(x => x > 0) > 1)
                throw new InvalidOperationException($"Distribua no maximo {budget} ponto(s) nas opcoes do predador" +
                    (chooseOne ? ", em uma unica opcao." : "."));
            for (var i = 0; i < options.Length; i++)
            {
                var max = MaximumFor(options[i].Name);
                if (allocation[i] > max || options[i].Name == "Isca de Estaca" && allocation[i] == 1)
                    throw new InvalidOperationException($"{options[i].Name}: use a pontuacao do catalogo (maximo {max}).");
                Grant(options[i], allocation[i]);
            }
        }
        for (var i = 0; i < definition.FixedGrants.Length; i++) Grant(definition.FixedGrants[i], definition.FixedPoints[i]);
        Allocate(definition.AllocationOptions, choice.AdvantageAllocation, definition.AllocationPoints, false);
        Allocate(definition.FlawOptions, choice.FlawAllocation, definition.FlawPoints, definition.FlawPoints > 0 && !definition.SplitFlaws);
        return grants;
    }

    public static AdvancementResult Remove(CharacterDetails details, IReadOnlyList<TraitValue> traits, bool requireSelection = true)
    {
        if (details.Advancement?.IsCreating != true) throw new InvalidOperationException("Trocas de predador sao permitidas somente durante a criacao.");
        if (details.PredatorSelection is not { } receipt)
        {
            if (requireSelection) throw new InvalidOperationException("Nao ha bonus de predador aplicado para remover.");
            return new(details, traits);
        }
        var hasDiscipline = !string.IsNullOrEmpty(receipt.DisciplineKey);
        var hasSkill = !string.IsNullOrEmpty(receipt.SkillKey);
        var discipline = hasDiscipline ? traits.SingleOrDefault(x => x.Category == TraitCategory.Discipline && x.Key == receipt.DisciplineKey) : null;
        var skill = hasSkill ? traits.SingleOrDefault(x => x.Category == TraitCategory.Skill && x.Key == receipt.SkillKey) : null;
        if (hasDiscipline && (discipline is null || discipline.Rating < 1) ||
            hasSkill && (skill is null || !skill.Specialization.Split([',', ';', '\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Any(x => ClanCatalog.SameName(x, receipt.Specialization))) ||
            receipt.Grants.Any(g => !traits.Contains(g)))
            throw new InvalidOperationException("Os tracos do predador foram alterados. Restaure a disciplina/habilidade antes de remover.");
        var values = traits.Where(x => !receipt.Grants.Any(g => x.Category == g.Category && x.Key == g.Key))
            .Select(x => discipline is not null && x == discipline ? x with { Rating = x.Rating - 1 } : skill is not null && x == skill && receipt.SpecializationAdded
                ? x with { Specialization = string.Join("\n", x.Specialization.Split([',', ';', '\r', '\n'],
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => !ClanCatalog.SameName(s, receipt.Specialization))) } : x).ToArray();
        var humanity = details.Humanity - receipt.HumanityDelta;
        var blood = details.BloodPotency - receipt.BloodDelta;
        if (humanity is < 0 or > 10 || blood is < 0 or > 10) throw new InvalidOperationException("A base original excede os limites; ajuste antes de trocar o predador.");
        return new(details with { PredatorSelection = null, Predator = "", Humanity = humanity, BloodPotency = blood }, values);
    }

    public static IReadOnlyList<ValidationIssue> Validate(CharacterDetails details, IReadOnlyList<TraitValue> traits)
    {
        if (details.PredatorSelection is not { } receipt)
            return traits.Any(IsGrant) ? [new(nameof(CharacterDetails.Predator), "Pontos de predador sem registro de origem.")] : [];
        var definition = PredatorCatalog.Find(receipt.Id);
        if (definition is null || receipt.Grants is null || receipt.Grants.Length > 20 ||
            receipt.Grants.Any(x => x is null || string.IsNullOrWhiteSpace(x.Key) || string.IsNullOrWhiteSpace(x.Name) ||
                x.Category is not (TraitCategory.Advantage or TraitCategory.Flaw) || x.Rating is < 1 or > 5 ||
                x.Specialization != "" || x.Loresheet is not null) ||
            receipt.DisciplineKey is null || receipt.Discipline is null || receipt.SkillKey is null || receipt.Skill is null ||
            receipt.Specialization is null ||
            string.IsNullOrWhiteSpace(receipt.DisciplineKey) != string.IsNullOrWhiteSpace(receipt.Discipline) ||
            string.IsNullOrWhiteSpace(receipt.SkillKey) != string.IsNullOrWhiteSpace(receipt.Skill) ||
            string.IsNullOrWhiteSpace(receipt.Skill) != string.IsNullOrWhiteSpace(receipt.Specialization) ||
            receipt.Specialization.Length > 120 ||
            string.IsNullOrWhiteSpace(details.Predator) ||
            receipt.Discipline.Length > 0 && !definition.Disciplines.Any(x => ClanCatalog.SameName(x, receipt.Discipline)) ||
            receipt.Skill.Length > 0 && !definition.Specializations.Any(x => ClanCatalog.SameName(x.Skill, receipt.Skill) &&
                (x.IsFreeText || ClanCatalog.SameName(x.Text, receipt.Specialization))) ||
            receipt.HumanityDelta != definition.HumanityDelta || receipt.BloodDelta != definition.BloodDelta ||
            !ClanCatalog.SameName(details.Predator, definition.Name))
            return [new(nameof(CharacterDetails.Predator), "Registro dos bonus do predador invalido ou nome diferente do predador aplicado.")];
        if (details.Advancement?.IsCreating != true) return [];
        try
        {
            var baseline = Remove(details, traits);
            var specIndex = receipt.Skill.Length == 0 ? -1 : Array.FindIndex(definition.Specializations, x => ClanCatalog.SameName(x.Skill, receipt.Skill) &&
                (x.IsFreeText || ClanCatalog.SameName(x.Text, receipt.Specialization)));
            int[] Allocation(PredatorAllocation[] options) => options.Select(option =>
                receipt.Grants.Where(x => x.Category == option.Category && ClanCatalog.SameName(x.Name, option.Name)).Sum(x => x.Rating)).ToArray();
            var expected = Apply(baseline.Details, baseline.Traits, new(receipt.Id, receipt.Discipline, specIndex,
                receipt.Specialization, Allocation(definition.AllocationOptions), Allocation(definition.FlawOptions)));
            if (expected.Details.PredatorSelection != receipt ||
                receipt.Grants.Any(g => !traits.Contains(g)) ||
                traits.Where(IsGrant).Any(x => !receipt.Grants.Contains(x)) ||
                receipt.SkillKey.Length > 0 && !traits.Any(x => x.Key == receipt.SkillKey && x.Category == TraitCategory.Skill &&
                    x.Specialization.Split([',', ';', '\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                        .Any(s => ClanCatalog.SameName(s, receipt.Specialization))))
                return [new(nameof(CharacterDetails.Predator), "Os bonus/custos do predador foram alterados. Remova e reaplique o predador antes de concluir.")];
        }
        catch (InvalidOperationException exception) { return [new(nameof(CharacterDetails.Predator), exception.Message)]; }
        return [];
    }
}
