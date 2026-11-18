using Game.Content.Disciplines;
using Game.Rules.Rituals;
using Game.Rules.V5.Blood;
using Game.Rules.V5.Dice;
using Game.Rules.V5.Disciplines;
using Game.Rules.V5.Rituals;
using Game.Sheet;

namespace Game.Application.Blood;

/// <summary>Pay and resolve core rituals once per saved stage, without mutating characters or world facts.</summary>
public sealed class RitualWorkflowActions
{
    private readonly IRitualEffectRuleset _ruleset;
    public RitualWorkflowActions(IRitualEffectRuleset? ruleset = null)
    {
        _ruleset = ruleset ?? new V5RitualEffectRuleset();
        if (!string.Equals(_ruleset.EditionId, "V5", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("O coordenador de custos e etapas de rituais instalado atende somente à V5.");
    }
    /// <summary>Validate the existing self-only beneficiary constraint before creating a ritual preparation.</summary>
    public static void ValidateBeneficiary(RitualUseDefinition ritual, bool targetsSelf, string target,
        bool exceptionApproved, string exceptionReason)
    {
        if (!targetsSelf && string.IsNullOrWhiteSpace(target))
            throw new InvalidOperationException("Identifique o alvo, objeto ou local deste preparo.");
        if (exceptionApproved && string.IsNullOrWhiteSpace(exceptionReason))
            throw new InvalidOperationException("Confirme o procedimento e justifique qualquer exceção com o Narrador.");
        if (!targetsSelf && (ritual.Kind is "clinging" or "deflection" or "incorporeal" or "heart-stone" or "nighthawk") && !exceptionApproved)
            throw new InvalidOperationException("Este ritual beneficia o conjurador; outro beneficiário exige exceção registrada.");
    }

    public RitualCostPayment PayCheck(RitualUseDefinition ritual, RitualWorkflowState state, int hunger, Random? random = null)
    {
        if (ritual.Kind is "sanctuary" or "ward" or "awakening")
            throw new InvalidOperationException("Use o procedimento dedicado deste ritual.");
        var requiredChecks = ritual.Kind == "dagon" ? state.Executions + 1 : ritual.RouseChecks;
        if (state.Resolved || state.ChecksPaid < 0 || state.ChecksPaid >= requiredChecks)
            throw new InvalidOperationException("Não há outra Checagem de Sangue a pagar nesta execução.");
        if (hunger is < 0 or >= 5) throw new InvalidOperationException("Fome 5 impede despertar o Sangue voluntariamente.");
        var check = RouseCheckRules.Roll(hunger, random);
        return new(state with { ChecksPaid = state.ChecksPaid + 1 }, check);
    }

    public RitualWorkflowResolution Resolve(RitualUseDefinition ritual, RitualWorkflowState state,
        RitualWorkflowContext context, Random? random = null)
    {
        RequireReady(ritual, state, context);
        if (state.Resolved) throw new InvalidOperationException("Esta etapa já foi resolvida. Inicie outra execução quando a regra permitir.");
        if (ritual.Kind == "bloodstone" && state.PreparationNights?.Count != 3 && !context.ExceptionApproved)
            throw new InvalidOperationException("Registre uma hora em cada uma das três noites antes do teste final da Pedra de Sangue.");
        if (ritual.Kind == "bloodstone" && context.ActiveBloodstones >= context.ResolveRating && !context.ExceptionApproved)
            throw new InvalidOperationException("O número de pedras ativas já alcança a Determinação; encerre um vínculo antes de criar outro.");
        if (ritual.Kind == "haven" && context.AreaRadius > 6 && !context.ExceptionApproved)
            throw new InvalidOperationException("Defesa do Refúgio Sagrado permite no máximo seis metros de raio.");
        if (ritual.Kind == "circle" && context.AreaRadius > 3 && !context.ExceptionApproved)
            throw new InvalidOperationException("O círculo padrão permite no máximo três metros de raio; registre uma exceção para alterar.");
        if (ritual.Kind == "heart-stone" && state.Stage == "preparing")
        {
            if (!context.CandleDamageConfirmed)
                throw new InvalidOperationException("Confirme que o ponto agravado da vela já foi contabilizado pela mesa.");
            var frenzy = FrenzyRules.RollResistance(FrenzyType.Terror, "Vela de Coração de Pedra", context.WillpowerPool, 3, context.Humanity, random);
            return new(state with { Stage = frenzy.Resisted ? "ritual" : "failed", Resolved = !frenzy.Resisted },
                new(frenzy.Resisted, frenzy.Resisted ? "Resistiu ao frenesi. Confirme novamente para rolar o Ritual, sem pagar outro custo." : "Frenesi de terror: o ritual termina sem benefício.", frenzy.Resisted ? "Aguardando teste do Ritual" : "Encerrado"), frenzy.Roll);
        }
        if (ritual.Kind == "circle" && !state.LongTermCircle)
            return new(state with { Stage = "circle-ready", Resolved = true },
                new(true, "Círculo preparado até o amanhecer. A disputa só ocorre quando um intruso tenta atravessar.", "Até o amanhecer"), null);
        var difficulty = ritual.Kind == "firewalker" ? 3 : ritual.Level + 1;
        if (ritual.Kind == "circle") difficulty += 2;
        if (ritual.Kind is "truth" or "dagon") difficulty = context.OpponentSuccesses + 1;
        var rollAudit = ApplyBeastToRoll(DicePoolRules.Roll(context.Pool, context.Hunger, difficulty, random),
            context.BeastSubdued);
        var roll = rollAudit.Roll;
        if (ritual.Kind == "circle")
            return new(state with { Stage = roll.IsSuccess ? "circle-ready" : "failed", Resolved = true,
                    StoredSuccesses = roll.Successes, StoredCritical = roll.IsCritical },
                new(roll.IsSuccess, roll.IsSuccess ? "Círculo de um ano e um dia preparado. Seu resultado fica registrado para as disputas de entrada." : "O preparo do círculo falhou.", roll.IsSuccess ? "Um ano e um dia" : "Encerrado"), roll,
                BeastSubduedConsidered: rollAudit.BeastSubduedConsidered,
                MessyCriticalConverted: rollAudit.MessyCriticalConverted);
        var proposal = _ruleset.Resolve(ritual, new(roll.Successes, roll.Difficulty, roll.IsCritical), context.OpponentSuccesses, context.VampireTarget);
        if (ritual.Kind == "dagon")
        {
            var executions = state.Executions + 1;
            return new(state with { Executions = executions, Stage = executions < 3 ? "repeat" : proposal.Succeeded ? "effect" : "failed",
                Resolved = executions >= 3, StoredSuccesses = roll.Successes, StoredCritical = roll.IsCritical }, proposal, roll,
                BeastSubduedConsidered: rollAudit.BeastSubduedConsidered,
                MessyCriticalConverted: rollAudit.MessyCriticalConverted);
        }
        var stage = proposal.Succeeded ? ritual.Kind switch { "truth" => "statements", "air" => "potion", _ => "effect" } : "failed";
        return new(state with { Stage = stage, Resolved = ritual.Kind != "truth" || !proposal.Succeeded,
            StoredSuccesses = roll.Successes, StoredCritical = roll.IsCritical, Charges = proposal.Charges }, proposal, roll,
            BeastSubduedConsidered: rollAudit.BeastSubduedConsidered,
            MessyCriticalConverted: rollAudit.MessyCriticalConverted);
    }

    public RitualWorkflowResolution CircleContact(RitualUseDefinition ritual, RitualWorkflowState state,
        RitualWorkflowContext context, string intruder, int intruderWillpowerPool,
        bool retry, bool retryCostConfirmed, Random? random = null)
    {
        RequireReady(ritual, state, context);
        if (ritual.Kind != "circle" || state.Stage != "circle-ready" || string.IsNullOrWhiteSpace(intruder))
            throw new InvalidOperationException("Selecione um círculo preparado e identifique o intruso elegível.");
        if (intruderWillpowerPool is < 0 or > 50) throw new ArgumentOutOfRangeException(nameof(intruderWillpowerPool));
        var previous = state.Intruders?.FirstOrDefault(x => string.Equals(x.Key, intruder.Trim(), StringComparison.OrdinalIgnoreCase));
        // Backward compatibility for an execution saved before the per-intruder ledger was available.
        if (previous is null && string.Equals(intruder.Trim(), state.LastIntruder, StringComparison.OrdinalIgnoreCase))
            previous = new(intruder.Trim(), state.LastCasterSuccesses, state.LastCasterCritical, state.LastIntruderAffected, false);
        if (previous is { Affected: true, Entered: false } && !retry)
            throw new InvalidOperationException("Este intruso já foi afetado; confirme a nova tentativa e o custo de Vontade.");
        if (retry && (previous is not { Affected: true, Entered: false } || !retryCostConfirmed))
            throw new InvalidOperationException("Uma nova tentativa exige um intruso afetado e 1 Vontade pago, confirmado pela mesa.");
        DicePoolResult? caster = null;
        RitualRollAudit? casterAudit = null;
        var successes = previous?.CasterSuccesses ?? state.StoredSuccesses;
        var critical = previous?.CasterCritical ?? state.StoredCritical;
        if (!state.LongTermCircle && previous is null)
        {
            casterAudit = ApplyBeastToRoll(DicePoolRules.Roll(context.Pool, context.Hunger, 0, random),
                context.BeastSubdued);
            caster = casterAudit.Roll;
            successes = caster.Successes; critical = caster.IsCritical;
        }
        var opposition = DicePoolRules.Roll(intruderWillpowerPool, 0, 0, random);
        var proposal = _ruleset.CircleContact(successes, critical, opposition.Successes);
        var affected = proposal.HealthDamage > 0 || previous?.Affected == true;
        // The ruleset owns the contest outcome, including the acting intruder winning a tie.
        var entered = !proposal.Succeeded || previous?.Entered == true;
        var intruders = (state.Intruders ?? []).Where(x => !string.Equals(x.Key, intruder.Trim(), StringComparison.OrdinalIgnoreCase))
            .Append(new RitualCircleIntruder(intruder.Trim(), successes, critical, affected, entered)).ToArray();
        return new(state with { LastIntruder = intruder.Trim(), LastCasterSuccesses = successes,
            LastCasterCritical = critical, LastIntruderAffected = affected, Intruders = intruders }, proposal, caster, opposition,
            casterAudit?.BeastSubduedConsidered ?? false, casterAudit?.MessyCriticalConverted ?? false);
    }

    /// <summary>Follow an already prepared trail without repaying its ritual cost or refreshing its duration.</summary>
    public RitualWorkflowResolution TrackPrey(PowerEffectEntry trail, int intelligence, int survival, int hunger,
        bool tableConfirmed, bool beastSubdued = false, Random? random = null)
    {
        if (trail is not { Active: true, RitualWorkflow: { Stage: "effect", Resolved: true } state } ||
            RitualUseCatalog.Find(trail.PowerId)?.Kind != "trail" || !tableConfirmed)
            throw new InvalidOperationException("Selecione um rastro ativo e confirme sua validade e a situação atual com o Narrador.");
        if (intelligence is < 0 or > 50 || survival is < 0 or > 50 || intelligence + survival > 50 ||
            hunger is < 0 or > 5 || trail.RemainingPoints is < 0 or > 50)
            throw new ArgumentOutOfRangeException(nameof(trail), "Confira os valores de Inteligência, Sobrevivência, Fome e dificuldade do rastro.");
        var rollAudit = ApplyBeastToRoll(
            DicePoolRules.Roll(intelligence + survival, hunger, trail.RemainingPoints, random), beastSubdued);
        var roll = rollAudit.Roll;
        var effect = roll.IsSuccess ? "O rastro foi seguido; o Narrador descreve o trajeto." : "O teste para seguir o rastro falhou.";
        return new(state, new(roll.IsSuccess, effect, trail.Duration), roll,
            BeastSubduedConsidered: rollAudit.BeastSubduedConsidered,
            MessyCriticalConverted: rollAudit.MessyCriticalConverted);
    }

    public RitualWorkflowResolution ConsumeAir(RitualUseDefinition ritual, RitualWorkflowState state, bool confirmed)
    {
        if (ritual.Kind != "air" || state.Stage != "potion" || state.Charges < 1 || !confirmed)
            throw new InvalidOperationException("Selecione uma poção com dose disponível e confirme consumo pelo conjurador antes de expirar.");
        return new(state with { Charges = state.Charges - 1 }, new(true,
            "Voo ou flutuação à velocidade de corrida por uma cena. Ao carregar massa de uma pessoa, use velocidade de caminhada; alvo involuntário exige disputa própria.", "Cena"), null);
    }

    public RitualNightEnding EndNighthawk(RitualUseDefinition ritual, RitualWorkflowState state,
        DateOnly ritualNight, bool eyesRemoved, bool tableConfirmed)
    {
        if (ritual.Kind != "nighthawk" || state is not { Stage: "effect", Resolved: true } || !tableConfirmed)
            throw new InvalidOperationException("Selecione o voo ativo e confirme o fim da noite e o estado dos olhos da ave com o Narrador.");
        var ending = RitualOngoingRules.EndNighthawk(eyesRemoved);
        var condition = ending.BlindnessNights == 0 ? null : new RitualWorkflowState(Stage: "blindness", Resolved: true,
            NightsRemaining: ending.BlindnessNights, LastNightAdvanced: ritualNight);
        return new(state with { Stage = "ended" }, new(true, ending.Effect, "Voo encerrado"), condition);
    }

    public RitualWorkflowResolution AdvanceNighthawkBlindness(RitualWorkflowState state, DateOnly completedNight, bool tableConfirmed)
    {
        if (state is not { Stage: "blindness", LastNightAdvanced: { } previousNight } || !tableConfirmed)
            throw new InvalidOperationException("Selecione a cegueira do ritual e confirme quais noites da crônica já terminaram.");
        var remaining = RitualOngoingRules.AdvanceNighthawkBlindness(state.NightsRemaining, previousNight, completedNight);
        return new(state with { NightsRemaining = remaining, LastNightAdvanced = completedNight,
                Stage = remaining == 0 ? "ended" : "blindness" },
            new(true, remaining == 0 ? "As três noites de cegueira foram concluídas, conforme confirmação da mesa."
                : $"Cegueira do ritual: {remaining} noite(s) restante(s).", remaining == 0 ? "Encerrado" : $"{remaining} noite(s)"), null);
    }

    /// <summary>Keep ritual state and paid costs from being discarded by the generic effect-ending action.</summary>
    public static string? GenericEndingBlockReason(PowerEffectEntry entry)
    {
        if (entry.PowerId == "ritual-eyes-of-nighthawk" && entry.RitualWorkflow is { Stage: "effect" or "blindness" })
            return "Use o encerramento de Olhos do Falcão Noturno: confirme os olhos da ave no fim da noite; a cegueira usa noites da crônica ou decisão excepcional registrada.";
        if (entry.PowerId == "ritual-sanctuary-preparation-check")
            return "As checagens pagas pertencem ao histórico do par de círculos. Para abandoná-lo, use o encerramento da preparação do Santuário.";
        if (entry.PowerId == "ritual-sanctuary-travel-attempt")
            return "O marcador de viagem termina ao encerrar a cena. Uma nova tentativa na mesma cena exige exceção justificada pelo Narrador.";
        return null;
    }

    /// <summary>End an active ritual blindness early only after a documented Narrator decision.</summary>
    public PowerEffectEntry EndNighthawkBlindnessByRuling(PowerEffectEntry entry, bool tableConfirmed,
        bool exceptionApproved, string reason)
    {
        if (entry is not { Active: true, PowerId: "ritual-eyes-of-nighthawk", RitualWorkflow: { Stage: "blindness" } state } ||
            !tableConfirmed || !exceptionApproved || string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Selecione a cegueira ativa, confirme a decisão e registre a autorização e a justificativa do Narrador.");
        var ruling = reason.Trim();
        return entry with { Active = false, RitualWorkflow = state with { Stage = "ended", NightsRemaining = 0 },
            Ruling = string.IsNullOrWhiteSpace(entry.Ruling) ? ruling : entry.Ruling + "\n" + ruling,
            Effect = "Cegueira encerrada por decisão excepcional do Narrador: " + ruling };
    }

    public RitualWorkflowState RecordBloodstoneNight(RitualWorkflowState state, DateOnly night, bool confirmed, bool exceptionApproved)
    {
        if (state.Resolved || state.ChecksPaid != 1 || !confirmed)
            throw new InvalidOperationException("Selecione a pedra em preparo, pague seu custo e confirme uma hora de preparação desta noite.");
        var nights = state.PreparationNights ?? [];
        if (nights.Count >= 3 || nights.Contains(night))
            throw new InvalidOperationException("Esta noite já foi registrada ou as três noites já estão completas.");
        if (nights.Count > 0 && night != nights[^1].AddDays(1) && !exceptionApproved)
            throw new InvalidOperationException("A próxima preparação ocorre na noite seguinte; uma alteração exige decisão registrada do Narrador.");
        return state with { PreparationNights = [.. nights, night] };
    }

    public RitualWorkflowResolution StrikeShaft(RitualWorkflowState state, int basePool, int hunger,
        int oppositionSuccesses, bool confirmed, Random? random = null, bool beastSubdued = false)
    {
        if (state.Stage != "effect" || !state.Resolved || !confirmed)
            throw new InvalidOperationException("Selecione uma estaca preparada e confirme o ataque contra vampiro com o Narrador.");
        if (basePool is < 0 or > 47 || hunger is < 0 or > 5 || oppositionSuccesses is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(basePool));
        var rollAudit = ApplyBeastToRoll(
            DicePoolRules.Roll(basePool + 3, hunger, oppositionSuccesses + 1, random), beastSubdued);
        var roll = rollAudit.Roll;
        var margin = roll.Successes - oppositionSuccesses;
        if (margin <= 0) return new(state, new(false, "O ataque não acertou; a estaca continua disponível.", "Até ser usada"), roll,
            BeastSubduedConsidered: rollAudit.BeastSubduedConsidered,
            MessyCriticalConverted: rollAudit.MessyCriticalConverted);
        var finalDeath = margin >= 5;
        return new(state with { Stage = finalDeath ? "final-death-proposed" : "splinter" },
            new(true, (finalDeath ? "Margem de pelo menos cinco: proposta de Morte Final em um turno; o Narrador confirma o alvo e a consequência."
                : "A estaca se quebrou: a lasca avança até o coração. O Narrador determina horas/noites conforme o local atingido; o livro não fornece velocidade numérica."),
                finalDeath ? "Consequência a confirmar" : "Até extração ou chegada ao coração"), roll,
            BeastSubduedConsidered: rollAudit.BeastSubduedConsidered,
            MessyCriticalConverted: rollAudit.MessyCriticalConverted);
    }

    public RitualWorkflowResolution RemoveShaftSplinter(RitualWorkflowState state, int pool, int hunger,
        bool confirmed, Random? random = null, bool beastSubdued = false)
    {
        if (state.Stage != "splinter" || !confirmed)
            throw new InvalidOperationException("Confirme que outra pessoa pode realizar Destreza + Medicina e que há até quatro horas disponíveis.");
        if (pool is < 0 or > 50 || hunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(pool));
        var rollAudit = ApplyBeastToRoll(DicePoolRules.Roll(pool, hunger, 6, random), beastSubdued);
        var roll = rollAudit.Roll;
        return new(state with { Stage = roll.IsSuccess ? "removed" : "splinter" },
            new(roll.IsSuccess, (roll.IsSuccess ? "Extração cirúrgica bem-sucedida; a lasca foi removida."
                : "A cirurgia falhou; a lasca permanece. Amputação pode removê-la antes de atingir o tronco, conforme a mesa."),
                roll.IsSuccess ? "Encerrado" : "Lasca ativa"), roll,
            BeastSubduedConsidered: rollAudit.BeastSubduedConsidered,
            MessyCriticalConverted: rollAudit.MessyCriticalConverted);
    }

    private static RitualRollAudit ApplyBeastToRoll(DicePoolResult originalRoll, bool beastSubdued)
    {
        var roll = QuellBeastRules.ApplyToRoll(originalRoll, beastSubdued);
        return new(roll, beastSubdued,
            originalRoll.Outcome == TestOutcome.MessyCritical && roll.Outcome == TestOutcome.CriticalSuccess);
    }

    private static void RequireReady(RitualUseDefinition ritual, RitualWorkflowState state, RitualWorkflowContext context)
    {
        if (ritual.Kind is "sanctuary" or "ward" or "awakening") throw new InvalidOperationException("Use o procedimento dedicado deste ritual.");
        if (!context.ConditionsConfirmed) throw new InvalidOperationException("Confirme tempo, ingredientes, alvo e gatilho com o Narrador.");
        var requiredChecks = ritual.Kind == "dagon" ? state.Executions + 1 : ritual.RouseChecks;
        if (state.ChecksPaid != requiredChecks) throw new InvalidOperationException("Pague as Checagens de Sangue restantes antes de resolver.");
        if (context.Pool is < 0 or > 50 || context.Hunger is < 0 or > 5 || context.OpponentSuccesses is < 0 or > 100 ||
            context.AreaRadius is < 0 or > 100 || context.ActiveBloodstones < 0 || context.ResolveRating is < 0 or > 5 ||
            context.WillpowerPool is < 0 or > 50 || context.Humanity is < 0 or > 10)
            throw new ArgumentOutOfRangeException(nameof(context));
    }
}

public sealed record RitualWorkflowContext(int Pool, int Hunger, int OpponentSuccesses, bool VampireTarget,
    bool ConditionsConfirmed, bool BeastSubdued, int AreaRadius, int ActiveBloodstones, int ResolveRating,
    int WillpowerPool, int Humanity, bool CandleDamageConfirmed, bool ExceptionApproved = false);
public sealed record RitualCostPayment(RitualWorkflowState State, RouseCheckResult Check);
public sealed record RitualWorkflowResolution(RitualWorkflowState State, RitualEffectProposal Proposal,
    DicePoolResult? Roll, DicePoolResult? OppositionRoll = null, bool BeastSubduedConsidered = false,
    bool MessyCriticalConverted = false);
public sealed record RitualNightEnding(RitualWorkflowState EndedState, RitualEffectProposal Proposal, RitualWorkflowState? Condition);
internal sealed record RitualRollAudit(DicePoolResult Roll, bool BeastSubduedConsidered,
    bool MessyCriticalConverted);
