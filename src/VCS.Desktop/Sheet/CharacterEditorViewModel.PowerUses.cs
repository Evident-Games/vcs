using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Application.Blood;
using VCS.Content.Disciplines;
using VCS.Rules.V5.Conflicts;
using VCS.Sheet;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    public ObservableCollection<PowerEffectEntry> PowerEffects { get; } = [];
    [ObservableProperty] private TraitItemViewModel? powerUseAttribute;
    [ObservableProperty] private TraitItemViewModel? powerUseTrait;
    public IReadOnlyList<TraitItemViewModel> PowerUseTraits => [.. Attributes.Items, .. Skills.Items, .. Disciplines.Items];
    [ObservableProperty] private PowerEffectEntry? selectedPowerEffect;
    [ObservableProperty] private DateTime powerGameDate = DateTime.Today;
    [ObservableProperty] private bool powerConditionsConfirmed;
    [ObservableProperty] private bool powerTargetIsSelf = true;
    [ObservableProperty] private bool powerTargetIsVampire = true;
    [ObservableProperty] private bool powerRollRequired;
    [ObservableProperty] private int powerUseDifficulty = 3;
    [ObservableProperty] private int powerOpponentSuccesses;
    [ObservableProperty] private int powerUseModifier;
    [ObservableProperty] private string powerRuling = "";
    [ObservableProperty] private bool defyBaneActive;
    [ObservableProperty] private int defyBaneRemaining;
    [ObservableProperty] private int unhealableSuperficial;
    [ObservableProperty] private bool eyesOfTheBeastActive;
    [ObservableProperty] private bool dauntActive;
    [ObservableProperty] private bool intimidationTargetIsMortal;
    [ObservableProperty] private bool ghostInTheMachineApplies;
    [ObservableProperty] private bool lethalBodyApplies;
    [ObservableProperty] private bool fistOfCaineActive;
    [ObservableProperty] private bool featherActive;
    [ObservableProperty] private bool incomingDamageIsFall;
    [ObservableProperty] private bool incomingDamageIsFireOrSupernatural;
    [ObservableProperty] private bool mistFormActive;
    [ObservableProperty] private bool isBiteAttack;
    [ObservableProperty] private int temporaryBloodPotencyBonus;

    public int EffectiveBloodPotency => Math.Clamp(BloodPotency + TemporaryBloodPotencyBonus, 0, 10);
    public PowerUseDefinition? SelectedPowerUse => ActivationPower is { } power ? PowerUseCatalog.Find(power.Key) : null;
    public string PowerUseConditions => SelectedPowerUse is { } use
        ? use.Conditions + (ActivationPower?.Entry?.Definition.DisciplineId == "discipline-dominate"
            ? " Dominação: confirme contato visual, compreensão e limites da ordem; geração inferior pode resistir com Vontade. Falha total bloqueia novas tentativas pela história. Exceções dependem dos poderes conhecidos ou da autorização registrada."
            : "")
        : "Poder próprio ou ritual: custos e sistema definidos pela mesa.";
    public string PowerUseDefense => SelectedPowerUse?.Defense ?? "";
    public int PowerUsePool => SelectedPowerUse is not null ? Math.Clamp(
        (PowerUseAttribute?.Rating ?? 0) + (PowerUseTrait?.Rating ?? 0) + VCS.Rules.V5.Blood.BloodPotencyEffects.At(EffectiveBloodPotency).DisciplinePowerBonus + PowerUseModifier, 0, 50) : 0;
    private int AttributeRating(string key) => Attributes.Items.FirstOrDefault(x => x.Key == "attribute-" + key)?.Rating ?? 0;
    public string PowerUsePoolSummary => SelectedPowerUse is { } use
        ? $"Parada: {PowerUsePool} dados. {ActivationPower?.DicePool}. Fonte: {use.Source}. Potência efetiva: {EffectiveBloodPotency}."
        : "Sem resolvedor de poder do livro base selecionado.";
    partial void OnPowerUseAttributeChanged(TraitItemViewModel? value) => RefreshPowerUse();
    partial void OnPowerUseTraitChanged(TraitItemViewModel? value) => RefreshPowerUse();
    partial void OnPowerTargetIsVampireChanged(bool value)
    {
        if (SelectedPowerUse?.Operation == "telepathy") ActivationWillpowerCost = value ? 1 : 0;
        PowerConditionsConfirmed = false;
    }
    partial void OnPowerTargetIsSelfChanged(bool value) => PowerConditionsConfirmed = false;
    partial void OnPowerUseModifierChanged(int value) => RefreshPowerUse();
    partial void OnTemporaryBloodPotencyBonusChanged(int value)
    {
        OnPropertyChanged(nameof(EffectiveBloodPotency));
        RefreshPowerUse();
        RefreshRollPool();
        OnBloodPotencyChanged(BloodPotency);
    }
    partial void OnDefyBaneRemainingChanged(int value) { if (_ready) MarkChanged(); }
    partial void OnUnhealableSuperficialChanged(int value) { if (_ready) MarkChanged(); }
    partial void OnEyesOfTheBeastActiveChanged(bool value) => RefreshRollPool();
    partial void OnIntimidationTargetIsMortalChanged(bool value) => RefreshRollPool();
    partial void OnGhostInTheMachineAppliesChanged(bool value) => RefreshRollPool();
    partial void OnDauntActiveChanged(bool value)
    {
        if (value) AweActive = false;
        RefreshRollPool();
    }
    private void RefreshPowerUse()
    {
        OnPropertyChanged(nameof(SelectedPowerUse));
        OnPropertyChanged(nameof(PowerUseConditions));
        OnPropertyChanged(nameof(PowerUseDefense));
        OnPropertyChanged(nameof(PowerUsePool));
        OnPropertyChanged(nameof(PowerUsePoolSummary));
    }

    private void PreparePowerUse()
    {
        PowerConditionsConfirmed = false;
        PowerFormProfileConfirmed = false;
        PowerRuling = "";
        PowerUseModifier = 0;
        ActivationWillpowerCost = SelectedPowerUse?.Operation == "telepathy" && PowerTargetIsVampire ? 1 : 0;
        PowerOpponentSuccesses = 0;
        if (SelectedPowerUse is { } use)
        {
            PowerUseAttribute = Attributes.Items.FirstOrDefault(x => x.Key == "attribute-" + use.Attribute);
            PowerUseTrait = PowerUseTraits.FirstOrDefault(x => x.Key == use.Trait);
            ActivationRouseChecks = use.RouseChecks;
            PowerRollRequired = use.Mode != "none";
            PowerUseDifficulty = use.Operation == "potency" ? EffectiveBloodPotency + 2 : use.Difficulty;
        }
        RefreshPowerUse();
    }

    private void ResolvePowerUse()
    {
        if (ActivationPower is not { } power || SelectedPowerUse is not { } use) return;
        if (!ActivationCostsConfirmed || !PowerConditionsConfirmed)
        { ActivationSummary = "Confira os custos, alvo, oposição e condições do poder antes de resolver."; return; }
        if (!PowerTargetIsSelf && string.IsNullOrWhiteSpace(ActivationTarget))
        { ActivationSummary = "Identifique o alvo deste uso."; return; }
        if (use.Operation == "famulus" && (PowerTargetIsSelf || string.IsNullOrWhiteSpace(ActivationTarget)))
        { ActivationSummary = "Informe o animal como outro alvo para registrar o vínculo de familiar."; return; }
        if (use.Operation == "famulus" && PowerEffects.Any(x => x.PowerId == power.Key && x.Target == ActivationTarget.Trim() &&
            (x.GameDate ?? x.CreatedUtc).Date == PowerGameDate.Date) && !ActivationExceptionApproved)
        { ActivationSummary = "Já há um gole registrado para este familiar nesta noite. Use outra noite ou registre a exceção."; return; }
        if (use.Operation == "famulus" && PowerEffects.Any(x => x.Active && x.PowerId == power.Key && x.Target != ActivationTarget.Trim()) && !ActivationExceptionApproved)
        { ActivationSummary = "Há outro familiar registrado. Encerre o vínculo anterior somente se ele morreu, ou registre a exceção do Narrador."; return; }
        var altered = power.HasWarning || power.Level != use.Level || ActivationRouseChecks != use.RouseChecks ||
            PowerRollRequired != (use.Mode != "none") || PowerUseModifier != 0 ||
            ActivationWillpowerCost != (use.Operation == "telepathy" && PowerTargetIsVampire ? 1 : 0) ||
            (PowerRollRequired && (PowerUseAttribute?.Key != "attribute-" + use.Attribute || PowerUseTrait?.Key != use.Trait));
        if ((use.Operation is "earthshock" or "vanish") && PowerEffects.Any(x => x.Active && x.PowerId == power.Key) && !ActivationExceptionApproved)
        { ActivationSummary = "Este poder já foi usado nesta cena. Encerre a cena ou registre autorização do Narrador."; return; }
        if (use.Operation == "mist" && (ActivationRouseChecks is < 1 or > 3) && !ActivationExceptionApproved)
        { ActivationSummary = "Forma de Névoa exige de uma a três checagens."; return; }
        if (use.Operation == "form" && !PowerFormProfileConfirmed)
        { ActivationSummary = "Confirme o perfil físico da forma animal com o Narrador antes de transformar."; return; }
        if ((altered || ActivationExceptionApproved) && string.IsNullOrWhiteSpace(PowerRuling))
        { ActivationSummary = "Registre o motivo do ajuste ou da autorização do Narrador (inclusive familiar, consentimento ou teste secreto)."; return; }
        if (power.HasWarning && !ActivationExceptionApproved)
        { ActivationSummary = power.Warning + " Confirme a exceção do Narrador."; return; }
        if (use.Operation == "extinguish" && !PowerTargetIsVampire || (use.Operation is "theft" or "possessionHuman") && PowerTargetIsVampire)
        {
            if (!ActivationExceptionApproved || string.IsNullOrWhiteSpace(PowerRuling))
            { ActivationSummary = "A natureza deste alvo não é permitida pela regra; registre uma exceção para continuar."; return; }
        }
        try
        {
            var discipline = power.Entry?.Definition.DisciplineId ?? "";
            var result = new PowerUseActions(_powerRulesets).Resolve(new(use, ActivationExceptionApproved ? power.Level : use.Level, PowerUsePool, PowerRollRequired,
                PowerUseDifficulty, PowerOpponentSuccesses, DisciplineRating(discipline), AttributeRating("wits"),
                PowerTargetIsVampire, Hunger, EffectiveBloodPotency, ActivationRouseChecks, ActivationWillpowerCost,
                WillpowerSuperficial, WillpowerAggravated, WillpowerCapacity,
                BeastSubdued: HasActivePower("power-animalism-quell-the-beast")));
            Hunger = result.Costs.Hunger;
            WillpowerSuperficial = result.Costs.WillpowerSuperficial;
            var outcome = result.Resolution;
            if (outcome.Succeeded && use.Operation == "potency" && PowerTargetIsSelf)
                outcome = outcome with { PotencyIncrease = Math.Max(TemporaryBloodPotencyBonus, outcome.PotencyIncrease) };
            if (outcome.Succeeded && use.Operation == "famulus")
            {
                var nights = PowerEffects.Where(x => x.PowerId == power.Key && x.Target == ActivationTarget.Trim())
                    .Select(x => (x.GameDate ?? x.CreatedUtc).Date).Append(PowerGameDate.Date).Distinct().Count();
                outcome = outcome with { Effect = $"Familiar: {Math.Min(3, nights)}/3 noites de alimentação registradas. " +
                    (nights >= 3 ? "Vínculo concluído; somente um familiar pode estar vinculado." : "Vínculo ainda em formação."),
                    Duration = "Até a morte do familiar" };
            }
            if (outcome.Succeeded && use.Operation == "draught")
            {
                var granted = Powers.PowerRows.Where(x => x.Entry?.Definition.DisciplineId == discipline &&
                    x.Level <= DisciplineRating(discipline) / 2 && x.Entry?.Definition.AmalgamDisciplineId is null).Select(x => x.Name);
                outcome = outcome with { Effect = outcome.Effect + " Poderes elegíveis: " + string.Join(", ", granted) + "." };
            }
            var target = PowerTargetIsSelf ? Name : ActivationTarget.Trim();
            ActivationSummary = $"{power.Name} → {target}: {outcome.Effect} Duração: {outcome.Duration}. " +
                (result.Roll is { } roll ? $"{roll.Successes} sucesso(s), {roll.Outcome}; margem {outcome.Margin}. " : "Sem teste, conforme condições confirmadas. ") +
                $"{use.Source}. Decisão: {PowerRuling}";
            var entry = new PowerEffectEntry(Guid.NewGuid(), power.Key, power.Name, target, PowerTargetIsSelf,
                DateTime.UtcNow, ActivationSummary, outcome.Duration, use.Source, PowerRuling,
                outcome.Succeeded && (use.Duration != "Instantâneo" || use.Operation == "earthshock"), outcome.Turns,
                outcome.HealthDamage, outcome.Aggravated, outcome.WillpowerDamage,
                outcome.HungerIncrease, outcome.PotencyIncrease, false,
                use.Operation == "defy" ? DisciplineRating("discipline-fortitude") : 0, 0,
                use.Operation == "form" ? PowerFormStrength : null,
                use.Operation == "form" ? PowerFormDexterity : null,
                use.Operation == "form" ? PowerFormStamina : null, PowerGameDate.Date, PoolBonus: outcome.PoolBonus);
            if (outcome.Succeeded && PowerTargetIsSelf && (use.Operation is "mist" or "form"))
            {
                foreach (var previous in PowerEffects.Where(x => x.Active && x.IsSelf &&
                    x.PowerId is "power-protean-mist-form" or "power-protean-shapechange" or "power-protean-metamorphosis").ToArray())
                    ReplacePowerEffect(previous, previous with { Active = false });
                MistFormActive = false;
            }
            // Refreshing a power replaces its active instance on the same target, never stacks it.
            if (outcome.Succeeded || use.Operation == "entrancement")
                foreach (var previous in PowerEffects.Where(x => x.Active && x.PowerId == entry.PowerId &&
                    x.Target == target && x.IsSelf == entry.IsSelf).ToArray())
                    ReplacePowerEffect(previous, previous with { Active = false });
            if (outcome.Succeeded && PowerTargetIsSelf && (power.Key is "power-presence-awe" or "power-presence-daunt"))
                foreach (var previous in PowerEffects.Where(x => x.Active && x.IsSelf && x.PowerId != power.Key &&
                    x.PowerId is "power-presence-awe" or "power-presence-daunt").ToArray())
                    ReplacePowerEffect(previous, previous with { Active = false });
            PowerEffects.Add(entry);
            SelectedPowerEffect = entry;
            if (outcome.Succeeded && PowerTargetIsSelf && use.Operation is not ("mist" or "form")) SetPowerState(power.Key, true);
            if (outcome.Succeeded && use.Operation == "heart" && PowerTargetIsSelf) TorporStaked = false;
            if (ActivationExceptionApproved || altered)
                ExceptionRegister.Record(power.Name, use.Conditions, PowerRuling);
            if (outcome.Succeeded && use.Operation == "defy" && PowerTargetIsSelf)
                DefyBaneRemaining = DisciplineRating("discipline-fortitude");
            if (outcome.Succeeded && use.Operation == "potency" && PowerTargetIsSelf)
                TemporaryBloodPotencyBonus = Math.Max(TemporaryBloodPotencyBonus, outcome.PotencyIncrease);
            Notebook.AppendLog("Usos de Disciplinas", ActivationSummary);
            var dice = result.Costs.Checks.SelectMany(check => check.RerolledDie is { } reroll
                ? new[] { new RolledDieDisplay(check.Die, "SANGUE"), new RolledDieDisplay(reroll, "SANGUE") }
                : new[] { new RolledDieDisplay(check.Die, "SANGUE") }).ToList();
            if (result.Roll is { } rolled)
            {
                dice.AddRange(rolled.NormalDice.Select(x => new RolledDieDisplay(x, "COMUM")));
                dice.AddRange(rolled.HungerDice.Select(x => new RolledDieDisplay(x, "FOME")));
            }
            ShowDiceRoll(power.Name, ActivationSummary, dice);
            ActivationCostsConfirmed = false;
            RefreshRollPool();
            MarkChanged();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { ActivationSummary = exception.Message; }
    }

    private void SetPowerState(string id, bool active)
    {
        switch (id)
        {
            case "power-protean-feral-weapons": FeralWeaponsActive = active; break;
            case "power-celerity-fleetness": FleetnessActive = active; break;
            case "power-fortitude-toughness": ToughnessActive = active; break;
            case "power-fortitude-prowess-from-pain": ProwessFromPainActive = active; break;
            case "power-fortitude-flesh-of-marble": FleshOfMarbleActive = active; MarbleFirstSourceAvailable = active; break;
            case "power-potence-prowess": ProwessActive = active; break;
            case "power-auspex-heightened-senses": HeightenedSensesActive = active; break;
            case "power-presence-awe": AweActive = active; if (active) DauntActive = false; break;
            case "power-presence-daunt": DauntActive = active; break;
            case "power-fortitude-fortify-the-inner-facade": InnerFacadeActive = active; break;
            case "power-fortitude-defy-bane": DefyBaneActive = active; if (!active) DefyBaneRemaining = 0; break;
            case "power-protean-eyes-of-the-beast": EyesOfTheBeastActive = active; break;
            case "power-potence-fist-of-caine": FistOfCaineActive = active; break;
            case "power-potence-lethal-body": LethalBodyApplies = active; break;
            case "power-protean-weight-of-the-feather": FeatherActive = active; break;
            case "power-protean-mist-form": MistFormActive = active; break;
            case "power-blood-sorcery-blood-of-potency": if (!active) TemporaryBloodPotencyBonus = 0; break;
        }
    }

    private void ReplacePowerEffect(PowerEffectEntry oldValue, PowerEffectEntry value)
    {
        var index = PowerEffects.IndexOf(oldValue);
        if (index < 0) return;
        PowerEffects[index] = value;
        if (SelectedPowerEffect?.Id == oldValue.Id) SelectedPowerEffect = value;
    }

    [RelayCommand]
    private void EndSelectedPowerEffect()
    {
        if (SelectedPowerEffect is not { Active: true } entry) return;
        ReplacePowerEffect(entry, entry with { Active = false });
        if (entry.IsSelf) SetPowerState(entry.PowerId, false);
        RefreshRollPool();
        Notebook.AppendLog("Usos de Disciplinas", $"Encerrado: {entry.Name} → {entry.Target}.");
        MarkChanged();
    }

    [RelayCommand]
    private void ApplySelectedPowerConsequences()
    {
        if (SelectedPowerEffect is not { IsSelf: true, ConsequencesApplied: false } entry) return;
        if (entry.HealthDamage > 0)
        {
            var damage = !entry.Aggravated && ToughnessActive
                ? Math.Max(1, entry.HealthDamage - DisciplineRating("discipline-fortitude")) : entry.HealthDamage;
            var health = _chronicleActions.ApplyDamage(HealthCapacity, HealthSuperficial, HealthAggravated,
                damage, entry.Aggravated ? ConflictDamageType.Aggravated : ConflictDamageType.Superficial);
            HealthSuperficial = health.Superficial;
            HealthAggravated = health.Aggravated;
        }
        if (entry.WillpowerDamage > 0)
        {
            var willpower = _chronicleActions.ApplyDamage(WillpowerCapacity, WillpowerSuperficial, WillpowerAggravated,
                (entry.WillpowerDamage + 1) / 2, ConflictDamageType.Superficial);
            WillpowerSuperficial = willpower.Superficial;
            WillpowerAggravated = willpower.Aggravated;
        }
        Hunger = Math.Min(5, Hunger + entry.HungerIncrease);
        ReplacePowerEffect(entry, entry with { ConsequencesApplied = true });
        Notebook.AppendLog("Usos de Disciplinas", $"Consequências de {entry.Name} aplicadas uma vez nesta ficha.");
        MarkChanged();
    }

    private IReadOnlyList<PowerEffectEntry> SnapshotPowerEffects() => PowerEffects.Select(entry =>
        !entry.IsSelf ? entry : entry.PowerId switch
        {
            "power-fortitude-defy-bane" => entry with { RemainingPoints = DefyBaneRemaining, ProtectedSuperficial = UnhealableSuperficial },
            "power-fortitude-flesh-of-marble" => entry with { TurnChargeUsed = !MarbleFirstSourceAvailable },
            "power-celerity-fleetness" => entry with { TurnChargeUsed = FleetnessDodgeUsed },
            _ => entry
        }).ToArray();

    private void RestorePowerEffects()
    {
        UnhealableSuperficial = Math.Min(HealthSuperficial, PowerEffects.Where(x => x.IsSelf)
            .Select(x => x.ProtectedSuperficial).DefaultIfEmpty(0).Max());
        foreach (var entry in PowerEffects.Where(x => x.Active && x.IsSelf))
        {
            if (entry.PowerId != "power-protean-mist-form" || entry.TurnsRemaining == 0)
                SetPowerState(entry.PowerId, true);
            if (entry.PowerId == "power-fortitude-flesh-of-marble") MarbleFirstSourceAvailable = !entry.TurnChargeUsed;
            if (entry.PowerId == "power-celerity-fleetness") FleetnessDodgeUsed = entry.TurnChargeUsed;
            if (entry.PowerId == "power-fortitude-defy-bane")
            { DefyBaneRemaining = entry.RemainingPoints; }
            if (entry.PowerId == "power-blood-sorcery-blood-of-potency") TemporaryBloodPotencyBonus = entry.PotencyIncrease;
        }
    }

    private void AdvancePowerTurn()
    {
        foreach (var entry in PowerEffects.Where(x => x.Active && x.TurnsRemaining > 0).ToArray())
        {
            var remaining = entry.TurnsRemaining - 1;
            var transform = entry.PowerId is "power-protean-mist-form" or "power-protean-shapechange" or "power-protean-metamorphosis";
            ReplacePowerEffect(entry, entry with { TurnsRemaining = remaining, Active = transform || remaining > 0 });
            if (remaining == 0 && entry.IsSelf) SetPowerState(entry.PowerId, transform);
        }
        RefreshRollPool();
        MarkChanged();
    }

    private void EndScenePowerEntries()
    {
        // Timed, permanent and night effects are ended explicitly; do not discard them at a scene boundary.
        foreach (var entry in PowerEffects.Where(x => x.Active).ToArray())
        {
            var duration = PowerUseCatalog.Find(entry.PowerId)?.Duration;
            if (entry.PowerId is "power-potence-earthshock" or "power-obfuscate-vanish" || duration is "Cena" or "Turno" or "Uma ação" or "Um ataque à distância" or "Um ataque de Briga ou Armas Brancas" or "Cena / turnos" or "Cena ou reserva esgotada")
            {
                ReplacePowerEffect(entry, entry with { Active = false });
                if (entry.IsSelf) SetPowerState(entry.PowerId, false);
            }
        }
        UnhealableSuperficial = 0;
        foreach (var entry in PowerEffects.Where(x => x.ProtectedSuperficial > 0).ToArray())
            ReplacePowerEffect(entry, entry with { ProtectedSuperficial = 0 });
        RestorePowerEffects();
        MarkChanged();
    }
}
