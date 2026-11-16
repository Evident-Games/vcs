using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Application.Blood;
using VCS.Desktop.Catalogs;
using VCS.Sheet;
using VCS.Rules.V5.Rituals;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    [ObservableProperty] private PowerRowViewModel? selectedPreparedRitual;
    [ObservableProperty] private bool ritualConditionsConfirmed;
    [ObservableProperty] private bool ritualExceptionApproved;
    [ObservableProperty] private string ritualExceptionReason = "";
    [ObservableProperty] private bool awakeningDangerConfirmed;
    [ObservableProperty] private bool sanctuaryTravelConfirmed;
    [ObservableProperty] private string wardObject = "";
    [ObservableProperty] private bool wardContactConfirmed;
    [ObservableProperty] private bool wardIntruderIsCaster;
    [ObservableProperty] private string wardIntruder = "";
    [ObservableProperty] private int wardRetryPool = 2;
    [ObservableProperty] private int wardRetryHunger;
    [ObservableProperty] private bool wardRetryCostConfirmed;
    [ObservableProperty] private string ritualPreparationSummary = "Selecione um ritual conhecido e confira suas condições.";

    partial void OnSelectedPreparedRitualChanged(PowerRowViewModel? value)
    {
        RitualConditionsConfirmed = false;
        RitualExceptionApproved = false;
        RitualExceptionReason = "";
        AwakeningDangerConfirmed = false;
        SanctuaryTravelConfirmed = false;
        RitualPreparationSummary = "Confira novamente os ingredientes, o alvo e as condições deste ritual.";
    }

    [RelayCommand]
    private void RollSanctuaryTravel()
    {
        if (SelectedPreparedRitual is not { } ritual ||
            ritual.Entry?.Definition is not { Id: "ritual-escape-to-true-sanctuary", IsRitual: true })
        { RitualPreparationSummary = "Selecione o ritual Fuga para o Verdadeiro Santuário conhecido nesta ficha."; return; }
        var alreadyAttempted = PowerEffects.Any(x => x.Active && x.IsSelf && x.PowerId == "ritual-sanctuary-travel-attempt");
        if ((ritual.HasWarning || alreadyAttempted) && (!RitualExceptionApproved || string.IsNullOrWhiteSpace(RitualExceptionReason)))
        { RitualPreparationSummary = (alreadyAttempted ? "Já houve uma tentativa de viagem nesta cena. " : ritual.Warning + " ") + "Autorize e justifique a exceção com o Narrador."; return; }
        if (!SanctuaryTravelConfirmed)
        { RitualPreparationSummary = "Confirme com o Narrador os círculos, a posição, a concentração e o limite por cena."; return; }
        try
        {
            var pool = Math.Clamp(AttributeRating("intelligence") + DisciplineRating("discipline-blood-sorcery"), 0, 50);
            var roll = new TrueSanctuaryActions().AttemptTravel(pool, Hunger, true, true, true, true, false,
                beastSubdued: HasActivePower("power-animalism-quell-the-beast"));
            if (ritual.HasWarning || alreadyAttempted)
                ExceptionRegister.Record(ritual.Name, alreadyAttempted ? "Nova tentativa de viagem na mesma cena" : ritual.Warning, RitualExceptionReason.Trim());
            if (!alreadyAttempted)
            {
                PowerEffects.Add(new PowerEffectEntry(Guid.NewGuid(), "ritual-sanctuary-travel-attempt", ritual.Name,
                    Name, true, DateTime.UtcNow, "Tentativa de viagem realizada nesta cena.", "Até o fim da cena",
                    "Vampiro: A Máscara V5 (Livro base), p. 281", "", true));
                MarkChanged();
            }
            RitualPreparationSummary = $"Viagem ao Santuário: {roll.Successes} sucesso(s), dificuldade {roll.Difficulty}: {roll.Outcome}. " +
                (roll.IsSuccess ? "A viagem pode ocorrer conforme a decisão da mesa." : "A tentativa falhou; não tente novamente nesta cena.") +
                " Nenhuma ficha foi movida. Fonte: Vampiro: A Máscara V5 (Livro base), p. 281.";
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            ShowDiceRoll(ritual.Name, RitualPreparationSummary,
                roll.NormalDice.Select(x => new RolledDieDisplay(x, "COMUM"))
                    .Concat(roll.HungerDice.Select(x => new RolledDieDisplay(x, "FOME"))));
            SanctuaryTravelConfirmed = false;
            RitualExceptionApproved = false;
            RitualExceptionReason = "";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    partial void OnSelectedPowerEffectChanged(PowerEffectEntry? value)
    {
        WardContactConfirmed = false;
        WardIntruderIsCaster = false;
        WardRetryCostConfirmed = false;
        WardIntruder = "";
    }

    partial void OnWardIntruderChanged(string value)
    {
        WardContactConfirmed = false;
        WardIntruderIsCaster = false;
        WardRetryCostConfirmed = false;
    }

    [RelayCommand]
    private void PrepareRitual()
    {
        if (SelectedPreparedRitual is not { } ritual) { RitualPreparationSummary = "Selecione um ritual."; return; }
        if (!RitualConditionsConfirmed) { RitualPreparationSummary = "Confirme ingredientes, alvo e condições com o Narrador."; return; }
        if (ritual.HasWarning && !RitualExceptionApproved)
        { RitualPreparationSummary = ritual.Warning + " Autorize e justifique a exceção para continuar."; return; }
        if (RitualExceptionApproved && string.IsNullOrWhiteSpace(RitualExceptionReason))
        { RitualPreparationSummary = "Informe a decisão do Narrador para registrar a exceção."; return; }
        if (ritual.Entry?.Definition is not { IsRitual: true } definition)
        { RitualPreparationSummary = "Ritual próprio: resolva custos e teste manualmente com o Narrador."; return; }
        var deferred = definition.Id is "ritual-ward-against-lupines" or "ritual-ward-against-cainites" or "ritual-wake-with-evenings-freshness";
        var isWard = definition.Id is "ritual-ward-against-lupines" or "ritual-ward-against-cainites";
        if (isWard && string.IsNullOrWhiteSpace(WardObject))
        { RitualPreparationSummary = "Informe o objeto protegido antes de pagar o custo."; return; }
        try
        {
            var pool = Math.Clamp(AttributeRating("intelligence") + DisciplineRating("discipline-blood-sorcery"), 0, 50);
            var result = new RitualActions().Prepare(definition.Level, Hunger, pool, deferred,
                beastSubdued: HasActivePower("power-animalism-quell-the-beast"), ritualId: definition.Id);
            Hunger = result.Check.HungerAfter;
            if (RitualExceptionApproved)
                ExceptionRegister.Record(ritual.Name, ritual.HasWarning ? ritual.Warning : "Exceção ao ritual", RitualExceptionReason.Trim());
            if (definition.Id == "ritual-wake-with-evenings-freshness")
            {
                foreach (var previous in PowerEffects.Where(x => x.Active && x.IsSelf && x.PowerId == definition.Id).ToArray())
                    ReplacePowerEffect(previous, previous with { Active = false });
                var pending = new PowerEffectEntry(Guid.NewGuid(), definition.Id, ritual.Name, Name, true,
                    DateTime.UtcNow, "Preparado; aguardando perigo durante o sono.", "Até o fim do sono diurno", definition.Source, "", true);
                PowerEffects.Add(pending);
                SelectedPowerEffect = pending;
                MarkChanged();
            }
            if (isWard)
            {
                var ward = new PowerEffectEntry(Guid.NewGuid(), definition.Id, ritual.Name, WardObject.Trim(), false,
                    DateTime.UtcNow, "Proteção preparada; aguardando primeiro contato elegível.",
                    "Até o objeto ser destruído ou quebrado", definition.Source, RitualExceptionReason, true,
                    RemainingPoints: definition.Level);
                PowerEffects.Add(ward);
                SelectedPowerEffect = ward;
                MarkChanged();
            }
            RitualPreparationSummary = $"{ritual.Name}: {result.Requirements.PreparationMinutes} min; dificuldade padrão {result.Requirements.Difficulty}; Fome {Hunger}. " +
                (result.Roll is { } roll ? $"{roll.Successes} sucesso(s): {roll.Outcome}. " :
                    definition.Id == "ritual-wake-with-evenings-freshness"
                        ? "Teste adiado até surgir perigo durante o sono; resolva o despertar com o Narrador. "
                        : "Proteção: teste adiado até o primeiro contato; registre o objeto e resolva o efeito com o Narrador. ") +
                $"Fonte: {definition.Source}. Confira exceções específicas do ritual.";
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            var dice = new List<RolledDieDisplay> { new(result.Check.Die, "SANGUE") };
            if (result.Roll is { } rolled)
            {
                dice.AddRange(rolled.NormalDice.Select(x => new RolledDieDisplay(x, "COMUM")));
                dice.AddRange(rolled.HungerDice.Select(x => new RolledDieDisplay(x, "FOME")));
            }
            ShowDiceRoll(ritual.Name, RitualPreparationSummary, dice);
            RitualConditionsConfirmed = false;
            RitualExceptionApproved = false;
            RitualExceptionReason = "";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void ResolveWardContact()
    {
        if (SelectedPowerEffect is not { Active: true } ward ||
            ward.PowerId is not ("ritual-ward-against-lupines" or "ritual-ward-against-cainites"))
        { RitualPreparationSummary = "Selecione nos efeitos uma proteção de objeto ativa."; return; }
        if (WardRules.ExcludesCaster(ward.PowerId, WardIntruderIsCaster))
        { RitualPreparationSummary = "A Proteção contra Cainitas não afeta o próprio conjurador. Nenhuma rolagem foi feita."; return; }
        if (!WardContactConfirmed)
        { RitualPreparationSummary = "Confirme com o Narrador o toque voluntário do intruso correspondente."; return; }
        if (string.IsNullOrWhiteSpace(WardIntruder))
        { RitualPreparationSummary = "Identifique o intruso para registrar este contato."; return; }
        var intruder = WardIntruder.Trim();
        if (ward.TurnChargeUsed)
        {
            if (ward.PoolBonus == 0)
            { RitualPreparationSummary = "Registro antigo sem resultado estruturado; consulte o histórico e resolva este contato com o Narrador."; return; }
            var damage = ward.PoolBonus - 1;
            if (damage is not (0 or 1 or 3))
            { RitualPreparationSummary = "Resultado de proteção inválido; confira o histórico com o Narrador."; return; }
            var contact = WardRules.ResolveLaterContact(damage);
            var retry = WardRules.RetryRequirements(damage == 3);
            RitualPreparationSummary = $"{ward.Name} em {ward.Target}; intruso {intruder}: resultado inicial mantido; dano agravado proposto {contact.AggravatedHealthDamage}. " +
                (contact.RequiresWillpowerToTouchAgain ? $"Para um intruso já afetado voltar a tocar, gaste {retry.WillpowerCost} Vontade e vença Vigor + Determinação, dificuldade {retry.Difficulty}. " : "A primeira rolagem falhou. ") +
                "Confirme cada intruso e aplique consequências manualmente. Nenhuma outra ficha foi alterada.";
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            WardContactConfirmed = false;
            return;
        }
        var pool = Math.Clamp(AttributeRating("intelligence") + DisciplineRating("discipline-blood-sorcery"), 0, 50);
        var attempt = new WardActions().ResolveFirstContact(ward.RemainingPoints, Hunger, pool, true, true,
            casterBeastSubdued: HasActivePower("power-animalism-quell-the-beast"),
            ritualId: ward.PowerId, intruderIsCaster: WardIntruderIsCaster);
        if (attempt.Roll is not { } roll) return;
        RitualPreparationSummary = $"{ward.Name} em {ward.Target}; intruso {intruder}: {roll.Successes} sucesso(s), dificuldade {roll.Difficulty}. " +
            $"Dano agravado proposto ao intruso: {attempt.Result.AggravatedHealthDamage}. Nenhuma outra ficha foi alterada. Fonte: {ward.Source}.";
        ReplacePowerEffect(ward, ward with { TurnChargeUsed = true, PoolBonus = attempt.Result.AggravatedHealthDamage + 1, Effect = RitualPreparationSummary });
        Notebook.AppendLog("Rituais", RitualPreparationSummary);
        ShowDiceRoll(ward.Name, RitualPreparationSummary,
            roll.NormalDice.Select(x => new RolledDieDisplay(x, "COMUM")).Concat(roll.HungerDice.Select(x => new RolledDieDisplay(x, "FOME"))));
        WardContactConfirmed = false;
        MarkChanged();
    }

    [RelayCommand]
    private void RollWardRetry()
    {
        if (SelectedPowerEffect is not { Active: true, TurnChargeUsed: true } ward ||
            ward.PowerId is not ("ritual-ward-against-lupines" or "ritual-ward-against-cainites") ||
            ward.PoolBonus is not (2 or 4))
        { RitualPreparationSummary = "Selecione uma proteção com resultado estruturado bem-sucedido."; return; }
        if (WardRules.ExcludesCaster(ward.PowerId, WardIntruderIsCaster))
        { RitualPreparationSummary = "A Proteção contra Cainitas não afeta o próprio conjurador. Nenhuma rolagem foi feita."; return; }
        if (!WardRetryCostConfirmed)
        { RitualPreparationSummary = "Confirme que o intruso já foi afetado e gastou um ponto de Vontade."; return; }
        if (string.IsNullOrWhiteSpace(WardIntruder))
        { RitualPreparationSummary = "Identifique o intruso para registrar esta tentativa."; return; }
        if (WardRetryPool is < 0 or > 50 || WardRetryHunger is < 0 or > 5)
        { RitualPreparationSummary = "Parada deve estar entre 0 e 50; Fome entre 0 e 5."; return; }
        var roll = new WardActions().RollRetry(ward.PoolBonus - 1, WardRetryPool, WardRetryHunger,
            ritualId: ward.PowerId, intruderIsCaster: WardIntruderIsCaster);
        RitualPreparationSummary = $"Nova tentativa de {WardIntruder.Trim()} contra {ward.Name} em {ward.Target}: {roll.Successes} sucesso(s), dificuldade {roll.Difficulty}, {roll.Outcome}. " +
            (roll.IsSuccess ? "O intruso pode tentar tocar novamente." : "O intruso não pode tentar tocar nesta ocasião.") +
            " Custo registrado pela mesa; nenhuma outra ficha foi alterada. Fonte: Livro base, p. 275.";
        Notebook.AppendLog("Rituais", RitualPreparationSummary);
        ShowDiceRoll("Resistência à proteção", RitualPreparationSummary,
            roll.NormalDice.Select(x => new RolledDieDisplay(x, "COMUM")).Concat(roll.HungerDice.Select(x => new RolledDieDisplay(x, "FOME"))));
        WardRetryCostConfirmed = false;
    }

    [RelayCommand]
    private void ResolveAwakeningDanger()
    {
        var pending = PowerEffects.LastOrDefault(x => x.Active && x.IsSelf && x.PowerId == "ritual-wake-with-evenings-freshness");
        if (pending is null) { RitualPreparationSummary = "Nenhum ritual de despertar preparado e ativo nesta ficha."; return; }
        if (!AwakeningDangerConfirmed) { RitualPreparationSummary = "Confirme que há perigo durante o sono com o Narrador."; return; }
        var pool = Math.Clamp(AttributeRating("intelligence") + DisciplineRating("discipline-blood-sorcery"), 0, 50);
        var result = new RitualActions().ResolveAwakening(Hunger, pool,
            beastSubdued: HasActivePower("power-animalism-quell-the-beast"));
        var roll = result.Roll;
        RitualPreparationSummary = $"Despertar: {roll.Successes} sucesso(s), dificuldade {roll.Difficulty}: {roll.Outcome}. " +
            result.Effect +
            " Fonte: Vampiro: A Máscara V5 (Livro base), p. 276.";
        ReplacePowerEffect(pending, pending with { Active = false, Effect = RitualPreparationSummary });
        Notebook.AppendLog("Rituais", RitualPreparationSummary);
        ShowDiceRoll("Ritual de despertar", RitualPreparationSummary,
            roll.NormalDice.Select(x => new RolledDieDisplay(x, "COMUM")).Concat(roll.HungerDice.Select(x => new RolledDieDisplay(x, "FOME"))));
        AwakeningDangerConfirmed = false;
        MarkChanged();
    }

    [RelayCommand]
    private void EndAwakeningSleep()
    {
        var pending = PowerEffects.LastOrDefault(x => x.Active && x.IsSelf && x.PowerId == "ritual-wake-with-evenings-freshness");
        if (pending is null) { RitualPreparationSummary = "Nenhum ritual de despertar pendente nesta ficha."; return; }
        RitualPreparationSummary = "Sono diurno encerrado; ritual de despertar pendente encerrado sem rolagem.";
        ReplacePowerEffect(pending, pending with { Active = false, Effect = RitualPreparationSummary });
        AwakeningDangerConfirmed = false;
        Notebook.AppendLog("Rituais", RitualPreparationSummary);
        MarkChanged();
    }
}
