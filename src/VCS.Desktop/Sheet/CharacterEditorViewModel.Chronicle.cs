using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Content;
using VCS.Rules.V5.Blood;
using VCS.Rules.V5.Conflicts;

namespace VCS.Desktop.Sheet;

public sealed record ConflictKindOption(ConflictKind Kind, string Label)
{
    public static IReadOnlyList<ConflictKindOption> All { get; } =
    [
        new(ConflictKind.Physical, "Físico"),
        new(ConflictKind.Social, "Social"),
        new(ConflictKind.Mental, "Mental")
    ];
}

public sealed record ConflictDamageTypeOption(ConflictDamageType Type, string Label)
{
    public static IReadOnlyList<ConflictDamageTypeOption> All { get; } =
    [
        new(ConflictDamageType.Superficial, "Superficial"),
        new(ConflictDamageType.Aggravated, "Agravado")
    ];
}

public partial class CharacterEditorViewModel
{
    partial void OnHealthSuperficialChanged(int value) { OnPropertyChanged(nameof(CanApplyConflictDamage)); RefreshRollPool(); }
    partial void OnHealthAggravatedChanged(int value) { OnPropertyChanged(nameof(CanApplyConflictDamage)); RefreshRollPool(); }

    public string ChronicleRuleSource => SourceAttribution.ForRule(typeof(BloodBondRules));
    public string ConflictRuleSource => SourceAttribution.ForRule(typeof(ConflictRules));
    public string TorporRuleSource => SourceAttribution.ForRule(typeof(TorporRules));

    [ObservableProperty] private string bloodBondRegnantId = "";
    [ObservableProperty] private string bloodBondThrallId = "";
    [ObservableProperty] private int bloodBondLevel;
    [ObservableProperty] private DateTime? bloodBondLastVitaeDate;
    [ObservableProperty] private DateTime? bloodBondEventDate = DateTime.Today;
    [ObservableProperty] private string bloodBondSummary = "Registre um gole de Vitae para atualizar o nível do vínculo.";
    public int BloodBondEffectiveLevel => BloodBondLastVitaeDate is { } last && BloodBondEventDate is { } date && date.Date >= last.Date
        ? _chronicleActions.BloodBondLevel(ToBondState(), DateOnly.FromDateTime(date))
        : BloodBondLevel;

    partial void OnBloodBondEventDateChanged(DateTime? value) => OnPropertyChanged(nameof(BloodBondEffectiveLevel));
    partial void OnBloodBondLevelChanged(int value) => OnPropertyChanged(nameof(BloodBondEffectiveLevel));
    partial void OnBloodBondLastVitaeDateChanged(DateTime? value) => OnPropertyChanged(nameof(BloodBondEffectiveLevel));

    private BloodBondState ToBondState() => new(
        string.IsNullOrWhiteSpace(BloodBondRegnantId) ? "regnant" : BloodBondRegnantId.Trim(),
        string.IsNullOrWhiteSpace(BloodBondThrallId) ? "thrall" : BloodBondThrallId.Trim(),
        Math.Clamp(BloodBondLevel, 0, 3),
        BloodBondLastVitaeDate is { } last ? DateOnly.FromDateTime(last) : null);

    [RelayCommand]
    private void RecordBloodBondDrink()
    {
        if (string.IsNullOrWhiteSpace(BloodBondRegnantId) || string.IsNullOrWhiteSpace(BloodBondThrallId) || BloodBondEventDate is not { } date)
        {
            BloodBondSummary = "Informe regente, vassalo e data do gole.";
            return;
        }

        try
        {
            var result = _chronicleActions.RecordVitae(ToBondState(), DateOnly.FromDateTime(date));
            BloodBondLevel = result.State.Level;
            BloodBondLastVitaeDate = result.State.LastVitaeDate?.ToDateTime(TimeOnly.MinValue);
            OnPropertyChanged(nameof(BloodBondEffectiveLevel));
            BloodBondSummary = result.CountedAsNewDrink
                ? result.BondCompleted ? "O vínculo chegou ao nível 3." : $"Gole registrado. Nível atual: {result.State.Level}/3."
                : $"Já havia um gole registrado nesta noite. Nível: {result.State.Level}/3.";
            Notebook.AppendLog("Vínculos de Sangue",
                $"{date:dd/MM/yyyy}: {BloodBondRegnantId.Trim()} → {BloodBondThrallId.Trim()}, nível {result.State.Level}/3.");
        }
        catch (ArgumentException exception)
        {
            BloodBondSummary = exception.Message;
        }
    }

    [ObservableProperty] private DateTime? torporEnteredAt = DateTime.Today;
    [ObservableProperty] private DateTime? torporCheckAt = DateTime.Today;
    [ObservableProperty] private bool torporStaked;
    [ObservableProperty] private string torporSummary = "Informe a data de entrada em Torpor para calcular o despertar natural.";
    [ObservableProperty] private int torporVitaeBloodPotency;
    [ObservableProperty] private int torporVitaeHungerSlaked = 1;

    [RelayCommand]
    private void CheckTorporVitae()
    {
        var canWake = _chronicleActions.CanWakeWithVitae(BloodPotency, TorporVitaeBloodPotency,
            TorporVitaeHungerSlaked, TorporStaked);
        TorporSummary = canWake
            ? "A Vitae de Potência maior sacia ao menos 1 de Fome: pode despertar o vampiro."
            : TorporStaked
                ? "A estaca impede este despertar."
                : "Não desperta: a Vitae precisa ter Potência maior e saciar ao menos 1 de Fome.";
        Notebook.AppendLog("Torpor", $"Verificação de Vitae em {DateTime.Now:dd/MM/yyyy HH:mm}: {TorporSummary}");
    }

    [RelayCommand]
    private void CalculateTorpor()
    {
        if (TorporEnteredAt is not { } entered || TorporCheckAt is not { } check)
        {
            TorporSummary = "Informe a data de entrada em Torpor e a data da consulta.";
            return;
        }
        var entryTime = new DateTimeOffset(DateTime.SpecifyKind(entered.Date, DateTimeKind.Local));
        var checkTime = new DateTimeOffset(DateTime.SpecifyKind(check.Date, DateTimeKind.Local));
        if (Humanity is < 1 or > 10)
        {
            TorporSummary = "A Humanidade precisa estar entre 1 e 10 para calcular Torpor.";
            return;
        }
        var wakeTime = _chronicleActions.NaturalTorporWakeTime(Humanity, entryTime);
        var canWake = _chronicleActions.CanWakeNaturally(Humanity, entryTime, checkTime, TorporStaked);
        TorporSummary = TorporStaked
            ? $"A estaca impede o despertar natural. Sem estaca, a duração pela Humanidade {Humanity} termina em {wakeTime:dd/MM/yyyy}."
            : canWake
                ? $"O prazo natural terminou em {wakeTime:dd/MM/yyyy}; o vampiro pode despertar."
                : $"Despertar natural previsto para {wakeTime:dd/MM/yyyy}.";
        Notebook.AppendLog("Torpor", $"Consulta em {check:dd/MM/yyyy}: {TorporSummary}");
    }

    public IReadOnlyList<ConflictKindOption> ConflictKinds => ConflictKindOption.All;
    public IReadOnlyList<ConflictDamageTypeOption> ConflictDamageTypes => ConflictDamageTypeOption.All;
    [ObservableProperty] private ConflictKindOption selectedConflictKind = ConflictKindOption.All[0];
    [ObservableProperty] private ConflictDamageTypeOption selectedConflictDamageType = ConflictDamageTypeOption.All[0];
    [ObservableProperty] private int firstConflictSuccesses;
    [ObservableProperty] private int secondConflictSuccesses;
    [ObservableProperty] private int firstConflictDamageModifier;
    [ObservableProperty] private int secondConflictDamageModifier;
    [ObservableProperty] private bool firstConflictTargetIsVampire;
    [ObservableProperty] private bool secondConflictTargetIsVampire;
    [ObservableProperty] private string conflictSummary = "Informe os sucessos dos dois lados para resolver uma troca oposta.";
    private ConflictExchange? _lastConflictExchange;
    public bool CanApplyConflictDamage => _lastConflictExchange is { Damage.ToSecond: > 0 } exchange &&
        (exchange.Kind == ConflictKind.Physical ? HealthAggravated < HealthCapacity : WillpowerAggravated < WillpowerCapacity);

    [RelayCommand]
    private void ResolveConflict()
    {
        try
        {
            var exchange = _chronicleActions.ResolveConflict(SelectedConflictKind.Kind,
                FirstConflictSuccesses, FirstConflictDamageModifier, SecondConflictSuccesses,
                SecondConflictDamageModifier + (FeralWeaponsActive ? 2 : 0) + (ProwessActive ? VCS.Rules.V5.Conflicts.DisciplineCombatRules.ProwessDamage(DisciplineRating("discipline-potence"), UsingMeleeWeapon) : 0), SelectedConflictDamageType.Type,
                FirstConflictTargetIsVampire && !FeralWeaponsActive, false);
            if (exchange.Kind == ConflictKind.Physical && exchange.Damage.Type == ConflictDamageType.Superficial)
                exchange = exchange with { Damage = exchange.Damage with { ToSecond =
                    VCS.Rules.V5.Conflicts.DisciplineCombatRules.SuperficialDamage(exchange.Damage.ToSecond,
                        DisciplineRating("discipline-fortitude"), ToughnessActive, SecondConflictTargetIsVampire) } };
            _lastConflictExchange = exchange;
            var result = exchange.IsTie
                ? $"Empate: ambos causam dano."
                : $"Venceu: {exchange.Winner}; margem {exchange.Margin}.";
            ConflictSummary = $"{result} Dano: lado 1 {exchange.Damage.ToFirst}, lado 2 {exchange.Damage.ToSecond} ({SelectedConflictDamageType.Label}).";
            OnPropertyChanged(nameof(CanApplyConflictDamage));
            Notebook.AppendLog("Conflitos", $"{DateTime.Now:dd/MM/yyyy HH:mm}: {SelectedConflictKind.Label}. {ConflictSummary}");
        }
        catch (ArgumentOutOfRangeException exception)
        {
            ConflictSummary = exception.Message;
        }
    }

    [RelayCommand]
    private void ApplyConflictDamage()
    {
        if (_lastConflictExchange is not { } exchange || !CanApplyConflictDamage) return;
        var physical = exchange.Kind == ConflictKind.Physical;
        if (physical && FleshOfMarbleActive && MarbleFirstSourceAvailable)
        {
            MarbleFirstSourceAvailable = false;
            if (VCS.Rules.V5.Conflicts.DisciplineCombatRules.MarbleStopsSource(true, true,
                IncomingSourceIsSunlight, IncomingAttackIsCritical))
            {
                ConflictSummary += " Pele de Mármore ignorou a primeira fonte de dano deste turno.";
                _lastConflictExchange = null;
                OnPropertyChanged(nameof(CanApplyConflictDamage));
                Notebook.AppendLog("Conflitos", ConflictSummary);
                return;
            }
        }
        var result = _chronicleActions.ApplyDamage(physical ? HealthCapacity : WillpowerCapacity,
            physical ? HealthSuperficial : WillpowerSuperficial, physical ? HealthAggravated : WillpowerAggravated,
            exchange.Damage.ToSecond, exchange.Damage.Type);
        if (physical) { HealthSuperficial = result.Superficial; HealthAggravated = result.Aggravated; }
        else { WillpowerSuperficial = result.Superficial; WillpowerAggravated = result.Aggravated; }
        ConflictSummary += " Dano aplicado; excesso superficial convertido em agravado quando a trilha ficou cheia.";
        if (result.IsImpaired(physical ? HealthCapacity : WillpowerCapacity))
            ConflictSummary += " Trilha cheia: verifique o estado debilitado e as consequências com o Narrador.";
        if (result.UnmarkedDamage > 0)
            ConflictSummary += $" {result.UnmarkedDamage} dano(s) além da capacidade: decisão do Narrador necessária.";
        _lastConflictExchange = null;
        OnPropertyChanged(nameof(CanApplyConflictDamage));
        Notebook.AppendLog("Conflitos", ConflictSummary);
    }
}
