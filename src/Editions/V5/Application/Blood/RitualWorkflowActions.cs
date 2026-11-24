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
        RitualWorkflowContext context, DateOnly ritualDate, Random? random = null)
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
            return new(state with { Stage = "circle-ready", Resolved = true, EffectStartedOn = ritualDate },
                new(true, "Círculo preparado até o amanhecer. A disputa só ocorre quando um intruso tenta atravessar.", "Até o amanhecer"), null);
        var difficulty = ritual.Kind == "firewalker" ? 3 : ritual.Level + 1;
        if (ritual.Kind == "circle") difficulty += 2;
        if (ritual.Kind is "truth" or "dagon")
            difficulty = ContestRules.ActingDifficulty(context.OpponentSuccesses);
        var rollAudit = ApplyBeastToRoll(DicePoolRules.Roll(context.Pool, context.Hunger, difficulty, random),
            context.BeastSubdued);
        var roll = rollAudit.Roll;
        if (ritual.Kind == "circle")
            return new(state with { Stage = roll.IsSuccess ? "circle-ready" : "failed", Resolved = true,
                    StoredSuccesses = roll.Successes, StoredCritical = roll.IsCritical,
                    EffectStartedOn = roll.IsSuccess ? ritualDate : state.EffectStartedOn },
                new(roll.IsSuccess, roll.IsSuccess ? "Círculo de um ano e um dia preparado. Seu resultado fica registrado para as disputas de entrada." : "O preparo do círculo falhou.", roll.IsSuccess ? "Um ano e um dia" : "Encerrado"), roll,
                BeastSubduedConsidered: rollAudit.BeastSubduedConsidered,
                MessyCriticalConverted: rollAudit.MessyCriticalConverted);
        var proposal = _ruleset.Resolve(ritual, new(roll.Successes, roll.Difficulty, roll.IsCritical), context.OpponentSuccesses, context.VampireTarget);
        if (ritual.Kind == "dagon")
        {
            var executions = state.Executions + 1;
            var completed = executions >= DagonRules.MaximumExecutions;
            return new(state with { Executions = executions, Stage = completed ? "completed" : "repeat",
                Resolved = completed, StoredSuccesses = roll.Successes, StoredCritical = roll.IsCritical }, proposal, roll,
                BeastSubduedConsidered: rollAudit.BeastSubduedConsidered,
                MessyCriticalConverted: rollAudit.MessyCriticalConverted);
        }
        var stage = proposal.Succeeded ? ritual.Kind switch
        {
            "truth" => "statements",
            "air" => "potion",
            // Deflection is rolled only when the first genuine staking attempt occurs.
            // A success consumes the protection just as a failure does; only the recorded proposal differs.
            "deflection" => "triggered",
            _ => "effect"
        } : "failed";
        return new(state with { Stage = stage, Resolved = ritual.Kind != "truth" || !proposal.Succeeded,
            StoredSuccesses = roll.Successes, StoredCritical = roll.IsCritical, Charges = proposal.Charges,
            EffectStartedOn = proposal.Succeeded ? ritualDate : state.EffectStartedOn }, proposal, roll,
            BeastSubduedConsidered: rollAudit.BeastSubduedConsidered,
            MessyCriticalConverted: rollAudit.MessyCriticalConverted);
    }

    public RitualWorkflowResolution CircleContact(RitualUseDefinition ritual, RitualWorkflowState state,
        RitualWorkflowContext context, string intruder, int intruderWillpowerPool,
        bool attemptingToExit, bool retry, bool retryCostConfirmed, Random? random = null)
    {
        RequireReady(ritual, state, context);
        if (ritual.Kind != "circle" || state.Stage != "circle-ready" || string.IsNullOrWhiteSpace(intruder))
            throw new InvalidOperationException("Selecione um círculo preparado e identifique o intruso elegível.");
        RitualEffectRules.ValidateCircleCrossingDirection(state.CirclePointsInward, attemptingToExit);
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
        var proposal = _ruleset.CircleContact(successes, critical, opposition.Successes, attemptingToExit);
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

    /// <summary>End the Sire communication only when the table confirms its ten minutes or a disturbance.</summary>
    public PowerEffectEntry EndSireCommunication(PowerEffectEntry entry, SireCommunicationEnding ending,
        bool tableConfirmed)
    {
        if (entry is not { Active: true, PowerId: "ritual-communicate-with-kindred-sire",
                RitualWorkflow: { Stage: "effect", Resolved: true } state } || !tableConfirmed)
            throw new InvalidOperationException("Selecione uma Comunicação com o Senhor ativa e confirme a condição de encerramento com a mesa.");

        return entry with
        {
            Active = false,
            RitualWorkflow = state with { Stage = "ended" },
            Effect = SireCommunicationRules.EndingEffect(ending),
            Duration = "Encerrado"
        };
    }

    /// <summary>Close Dagon's Call when the caster declines an optional paid repetition.</summary>
    public PowerEffectEntry FinishDagonRepetitions(PowerEffectEntry entry, bool tableConfirmed)
    {
        if (entry is not { Active: true, PowerId: "ritual-dagons-call",
                RitualWorkflow: { Stage: "repeat", Resolved: false } state } || !tableConfirmed)
            throw new InvalidOperationException("Selecione O Chamado de Dagon aguardando repetição e confirme a decisão com a mesa.");

        return entry with
        {
            Active = false,
            RitualWorkflow = state with { Stage = "completed", Resolved = true },
            Effect = DagonRules.CompletionEffect(state.Executions),
            Duration = "Encerrado"
        };
    }

    /// <summary>End a still-active Truth of Blood sequence when its scene and mixture end.</summary>
    public PowerEffectEntry EndTruthOfBloodAfterScene(PowerEffectEntry entry, bool tableConfirmed)
    {
        if (entry is not { Active: true, PowerId: "ritual-truth-of-blood",
                RitualWorkflow: { Stage: "statements", Resolved: false } state } || !tableConfirmed)
            throw new InvalidOperationException("Selecione Verdade do Sangue com afirmações ativas e confirme o fim da cena com a mesa.");

        return entry with
        {
            Active = false,
            RitualWorkflow = state with { Stage = "ended", Resolved = true },
            Effect = TruthOfBloodRules.SceneEndingEffect,
            Duration = "Encerrado"
        };
    }

    public RitualWorkflowResolution ConsumeAir(RitualUseDefinition ritual, RitualWorkflowState state, bool confirmed)
    {
        if (ritual.Kind != "air" || state.Stage != "potion" || state.Charges < 1 || !confirmed)
            throw new InvalidOperationException("Selecione uma poção com dose disponível e confirme consumo pelo conjurador antes de expirar.");
        return new(state with { Charges = state.Charges - 1 }, new(true,
            "Voo ou flutuação à velocidade de corrida por uma cena. Ao carregar massa de uma pessoa, use velocidade de caminhada; alvo involuntário exige disputa própria.", "Cena"), null);
    }

    /// <summary>Discard every unused dose after the table confirms that the potion's night has ended.</summary>
    public RitualWorkflowResolution ExpireAirPotion(RitualUseDefinition ritual, RitualWorkflowState state,
        DateOnly completedNight, bool tableConfirmed)
    {
        if (ritual.Kind != "air" || state is not { Stage: "potion", Resolved: true, Charges: > 0 } || !tableConfirmed)
            throw new InvalidOperationException("Selecione uma poção ativa de Essência do Ar com doses restantes e confirme o fim da noite da crônica.");

        var discarded = state.Charges;
        return new(state with { Stage = "expired", Charges = 0, LastNightAdvanced = completedNight },
            new(true, $"Fim da noite de {completedNight:dd/MM/yyyy} confirmado: {discarded} dose(s) não consumida(s) perderam a potência.", "Encerrado"), null);
    }

    /// <summary>End incorporeality only after the table confirms why and where materialization happened.</summary>
    public PowerEffectEntry EndIncorporealPassage(PowerEffectEntry entry, bool shardReleased,
        bool insideSolid, bool tableConfirmed, string narratorResolution)
    {
        if (entry is not { Active: true, PowerId: "ritual-incorporeal-passage",
                RitualWorkflow: { Stage: "effect", Resolved: true } state } || !tableConfirmed)
            throw new InvalidOperationException("Selecione uma Passagem Incorpórea ativa e confirme o fim da cena ou que o caco foi largado.");
        if (insideSolid && string.IsNullOrWhiteSpace(narratorResolution))
            throw new InvalidOperationException("Materializar dentro de matéria sólida exige registrar a decisão do Narrador sobre o destino exato.");

        var trigger = shardReleased ? "o caco foi largado" : "a cena terminou";
        var ruling = narratorResolution.Trim();
        var effect = insideSolid
            ? $"Materialização confirmada porque {trigger}, dentro de matéria sólida. Decisão do Narrador: {ruling}"
            : $"Materialização confirmada porque {trigger}, fora de matéria sólida.";
        return entry with
        {
            Active = false,
            RitualWorkflow = state with { Stage = "ended" },
            Effect = effect,
            Duration = "Encerrado",
            Ruling = insideSolid
                ? string.IsNullOrWhiteSpace(entry.Ruling) ? ruling : entry.Ruling + "\n" + ruling
                : entry.Ruling
        };
    }

    /// <summary>End the haven protection only for one of the two endings defined by the V5 ritual.</summary>
    public PowerEffectEntry EndSacredHaven(PowerEffectEntry entry, SacredHavenEnding ending,
        bool tableConfirmed)
    {
        if (entry is not { Active: true, PowerId: "ritual-defense-of-sacred-haven",
                RitualWorkflow: { Stage: "effect", Resolved: true } state } || !tableConfirmed)
            throw new InvalidOperationException("Selecione uma Defesa do Refúgio Sagrado ativa e confirme a condição de encerramento com a mesa.");

        var effect = ending switch
        {
            SacredHavenEnding.DayEnded => "O dia protegido terminou; a Defesa do Refúgio Sagrado foi encerrada.",
            SacredHavenEnding.CasterLeftArea => "O conjurador deixou a área; a Defesa do Refúgio Sagrado foi encerrada imediatamente.",
            _ => throw new ArgumentOutOfRangeException(nameof(ending))
        };
        return entry with
        {
            Active = false,
            RitualWorkflow = state with { Stage = "ended" },
            Effect = effect,
            Duration = "Encerrado"
        };
    }

    /// <summary>Expire an untriggered Deflection only for one of the two endings defined by the ritual.</summary>
    public PowerEffectEntry EndDeflection(PowerEffectEntry entry, DeflectionEnding ending,
        bool tableConfirmed)
    {
        if (entry is not { Active: true, PowerId: "ritual-deflection-of-wooden-doom",
                RitualWorkflow: { Resolved: false } state } || !tableConfirmed)
            throw new InvalidOperationException("Selecione uma Deflecção ainda aguardando a primeira tentativa genuína e confirme a condição de encerramento com a mesa.");

        var effect = ending switch
        {
            DeflectionEnding.NightEnded => "O fim da noite foi confirmado; a proteção da Deflecção expirou sem ser acionada.",
            DeflectionEnding.SplinterRemoved => "A lasca foi removida de sob a língua; a proteção da Deflecção foi encerrada sem ser acionada.",
            _ => throw new ArgumentOutOfRangeException(nameof(ending))
        };
        return entry with
        {
            Active = false,
            RitualWorkflow = state with { Stage = "ended", Resolved = true },
            Effect = effect,
            Duration = "Encerrado"
        };
    }

    /// <summary>Expire Firewalker only after the table confirms that its chronicle night ended.</summary>
    public PowerEffectEntry EndFirewalker(PowerEffectEntry entry, DateOnly completedNight,
        bool tableConfirmed)
    {
        if (entry is not { Active: true, PowerId: "ritual-firewalker",
                RitualWorkflow: { Stage: "effect", Resolved: true } state } || !tableConfirmed)
            throw new InvalidOperationException("Selecione um Andarilho do Fogo ativo e confirme o fim da noite da crônica com a mesa.");

        return entry with
        {
            Active = false,
            RitualWorkflow = state with { Stage = "ended", LastNightAdvanced = completedNight },
            Effect = $"Fim da noite de {completedNight:dd/MM/yyyy} confirmado; a resistência de Andarilho do Fogo foi encerrada.",
            Duration = "Encerrado"
        };
    }

    /// <summary>End Clinging according to the duration stored by its original ritual roll.</summary>
    public PowerEffectEntry EndClinging(PowerEffectEntry entry, ClingingEnding ending,
        DateOnly completedNight, bool tableConfirmed)
    {
        if (entry is not { Active: true, PowerId: "ritual-clinging-of-the-insect",
                RitualWorkflow: { Stage: "effect", Resolved: true } state } || !tableConfirmed)
            throw new InvalidOperationException("Selecione uma Aderência do Inseto ativa e confirme a condição de encerramento com a mesa.");
        if (state.StoredCritical && ending != ClingingEnding.NightEnded)
            throw new InvalidOperationException("A vitória crítica estende Aderência do Inseto pela noite inteira; confirme o fim da noite.");
        if (!state.StoredCritical && ending != ClingingEnding.SceneEnded)
            throw new InvalidOperationException("Sem vitória crítica, Aderência do Inseto termina ao fim da cena; confirme o fim da cena.");

        var effect = ending == ClingingEnding.NightEnded
            ? $"Fim da noite de {completedNight:dd/MM/yyyy} confirmado; Aderência do Inseto crítica foi encerrada."
            : "Fim da cena confirmado; Aderência do Inseto foi encerrada.";
        return entry with
        {
            Active = false,
            RitualWorkflow = state with
            {
                Stage = "ended",
                LastNightAdvanced = ending == ClingingEnding.NightEnded ? completedNight : state.LastNightAdvanced
            },
            Effect = effect,
            Duration = "Encerrado"
        };
    }

    /// <summary>Expire Eyes of Babel from the confirmed chronicle date stored with its successful ritual roll.</summary>
    public PowerEffectEntry ExpireEyesOfBabel(PowerEffectEntry entry, DateOnly currentDate, bool tableConfirmed,
        bool exceptionApproved, string exceptionReason)
    {
        if (entry is not { Active: true, PowerId: "ritual-eyes-of-babel",
                RitualWorkflow: { Stage: "effect", Resolved: true } state } || !tableConfirmed)
            throw new InvalidOperationException("Selecione Olhos de Babel ativo e confirme a data atual da crônica com a mesa.");

        var startedOn = state.EffectStartedOn ?? (entry.GameDate is { } legacyDate
            ? DateOnly.FromDateTime(legacyDate)
            : null);
        if (startedOn is null)
            return EndTimedRitualByRuling(entry, state, currentDate, exceptionApproved, exceptionReason,
                "O registro antigo não informa quando o efeito começou", "Olhos de Babel");
        var confirmedStart = startedOn.Value;
        var expiresOn = state.StoredCritical ? confirmedStart.AddMonths(1) : confirmedStart.AddDays(7);
        if (currentDate < expiresOn)
            return EndTimedRitualByRuling(entry, state, currentDate, exceptionApproved, exceptionReason,
                $"Olhos de Babel ainda está ativo: começou em {confirmedStart:dd/MM/yyyy} e termina em {expiresOn:dd/MM/yyyy}",
                "Olhos de Babel");

        return entry with
        {
            Active = false,
            RitualWorkflow = state with { Stage = "ended", LastNightAdvanced = currentDate },
            Effect = $"Data da crônica {currentDate:dd/MM/yyyy} confirmada; Olhos de Babel terminou após {(state.StoredCritical ? "um mês" : "uma semana")}.",
            Duration = "Encerrado"
        };
    }

    /// <summary>End an illuminated trail only after the table confirms the last night covered by the ritual.</summary>
    public PowerEffectEntry ExpireIlluminatedTrail(PowerEffectEntry entry, DateOnly completedNight,
        bool tableConfirmed, bool exceptionApproved, string exceptionReason)
    {
        if (entry is not { Active: true, PowerId: "ritual-illuminate-trail-of-prey",
                RitualWorkflow: { Stage: "effect", Resolved: true } state } || !tableConfirmed)
            throw new InvalidOperationException("Selecione um rastro iluminado ativo e confirme com a mesa qual noite da crônica terminou.");

        var startedOn = state.EffectStartedOn ?? (entry.GameDate is { } legacyDate
            ? DateOnly.FromDateTime(legacyDate)
            : null);
        if (startedOn is null)
            return EndTimedRitualByRuling(entry, state, completedNight, exceptionApproved, exceptionReason,
                "O registro antigo não informa a noite em que o rastro foi iluminado", "Rastro iluminado");

        var finalNight = IlluminatedTrailRules.ExpiresAfterNight(startedOn.Value, state.StoredCritical);
        if (completedNight < finalNight)
            return EndTimedRitualByRuling(entry, state, completedNight, exceptionApproved, exceptionReason,
                $"O rastro ainda está visível: começou em {startedOn.Value:dd/MM/yyyy} e inclui a noite de {finalNight:dd/MM/yyyy}",
                "Rastro iluminado");

        return entry with
        {
            Active = false,
            RitualWorkflow = state with { Stage = "ended", LastNightAdvanced = completedNight },
            Effect = $"Fim da noite de {completedNight:dd/MM/yyyy} confirmado; o rastro iluminado deixou de ser visível.",
            Duration = "Encerrado"
        };
    }

    /// <summary>End a Bloodstone bond after one week or when the table confirms that the stone was destroyed.</summary>
    public PowerEffectEntry EndBloodstone(PowerEffectEntry entry, BloodstoneEnding ending, DateOnly currentDate,
        bool tableConfirmed, bool exceptionApproved, string exceptionReason)
    {
        if (entry is not { Active: true, PowerId: "ritual-craft-bloodstone",
                RitualWorkflow: { Stage: "effect", Resolved: true } state } || !tableConfirmed)
            throw new InvalidOperationException("Selecione uma Pedra de Sangue ativa e confirme a condição de encerramento com a mesa.");

        if (ending == BloodstoneEnding.Destroyed)
            return entry with
            {
                Active = false,
                RitualWorkflow = state with { Stage = "ended", LastNightAdvanced = currentDate },
                Effect = $"Destruição da Pedra de Sangue confirmada em {currentDate:dd/MM/yyyy}; o vínculo foi encerrado.",
                Duration = "Encerrado: pedra destruída"
            };

        var startedOn = state.EffectStartedOn ?? (entry.GameDate is { } legacyDate
            ? DateOnly.FromDateTime(legacyDate)
            : null);
        if (startedOn is null)
            return EndTimedRitualByRuling(entry, state, currentDate, exceptionApproved, exceptionReason,
                "O registro antigo não informa quando o vínculo começou", "Vínculo da Pedra de Sangue");
        var expiresOn = startedOn.Value.AddDays(7);
        if (currentDate < expiresOn)
            return EndTimedRitualByRuling(entry, state, currentDate, exceptionApproved, exceptionReason,
                $"O vínculo ainda está ativo: começou em {startedOn.Value:dd/MM/yyyy} e termina em {expiresOn:dd/MM/yyyy}",
                "Vínculo da Pedra de Sangue");

        return entry with
        {
            Active = false,
            RitualWorkflow = state with { Stage = "ended", LastNightAdvanced = currentDate },
            Effect = $"Data da crônica {currentDate:dd/MM/yyyy} confirmada; a semana da Pedra de Sangue terminou.",
            Duration = "Encerrado: uma semana concluída"
        };
    }

    /// <summary>End a protection circle only at its confirmed V5 duration boundary.</summary>
    public PowerEffectEntry ExpireProtectionCircle(PowerEffectEntry entry, DateOnly currentDate,
        bool tableConfirmed, bool exceptionApproved, string exceptionReason)
    {
        if (entry is not { Active: true, RitualWorkflow: { Stage: "circle-ready", Resolved: true } state } ||
            RitualUseCatalog.Find(entry.PowerId)?.Kind != "circle" || !tableConfirmed)
            throw new InvalidOperationException("Selecione um Círculo de Proteção ativo e confirme o amanhecer ou a data atual da crônica com a mesa.");

        if (!state.LongTermCircle)
            return entry with
            {
                Active = false,
                RitualWorkflow = state with { Stage = "ended", LastNightAdvanced = currentDate },
                Effect = $"Amanhecer de {currentDate:dd/MM/yyyy} confirmado; o Círculo de Proteção se dissipou.",
                Duration = "Encerrado: amanhecer confirmado"
            };

        var startedOn = state.EffectStartedOn ?? (entry.GameDate is { } legacyDate
            ? DateOnly.FromDateTime(legacyDate)
            : null);
        if (startedOn is null)
            return EndTimedRitualByRuling(entry, state, currentDate, exceptionApproved, exceptionReason,
                "O registro antigo não informa quando o círculo de um ano e um dia começou", "Círculo de Proteção");

        var expiresOn = ProtectionCircleDurationRules.ExpiresOn(startedOn.Value);
        if (currentDate < expiresOn)
            return EndTimedRitualByRuling(entry, state, currentDate, exceptionApproved, exceptionReason,
                $"O círculo ainda está ativo: começou em {startedOn.Value:dd/MM/yyyy} e termina em {expiresOn:dd/MM/yyyy}",
                "Círculo de Proteção");

        return entry with
        {
            Active = false,
            RitualWorkflow = state with { Stage = "ended", LastNightAdvanced = currentDate },
            Effect = $"Data da crônica {currentDate:dd/MM/yyyy} confirmada; o ano e um dia do Círculo de Proteção terminaram.",
            Duration = "Encerrado: um ano e um dia concluídos"
        };
    }

    private static PowerEffectEntry EndTimedRitualByRuling(PowerEffectEntry entry, RitualWorkflowState state,
        DateOnly currentDate, bool exceptionApproved, string exceptionReason, string reason, string effectName)
    {
        if (!exceptionApproved || string.IsNullOrWhiteSpace(exceptionReason))
            throw new InvalidOperationException(reason + "; apenas uma decisão excepcional justificada do Narrador pode encerrar este registro.");
        var ruling = exceptionReason.Trim();
        return entry with
        {
            Active = false,
            RitualWorkflow = state with { Stage = "ended", LastNightAdvanced = currentDate },
            Effect = $"{effectName} encerrado por decisão excepcional do Narrador em {currentDate:dd/MM/yyyy}: {ruling}",
            Duration = "Encerrado por decisão do Narrador",
            Ruling = string.IsNullOrWhiteSpace(entry.Ruling) ? ruling : entry.Ruling + " | Encerramento: " + ruling
        };
    }

    /// <summary>Resolve the caster's side when attempting to carry an unwilling passenger.</summary>
    public RitualWorkflowResolution ResolveAirPassengerContest(RitualUseDefinition ritual, RitualWorkflowState state,
        string passenger, int casterPool, int casterHunger, int opponentSuccesses, bool confirmed, Random? random = null,
        bool beastSubdued = false)
    {
        if (ritual.Kind != "air" || state is not { Stage: "effect", Resolved: true } ||
            string.IsNullOrWhiteSpace(passenger) || !confirmed)
            throw new InvalidOperationException("Selecione um consumo ativo de Essência do Ar e confirme o passageiro involuntário e a oposição com o Narrador.");
        if (casterPool is < 0 or > 50 || casterHunger is < 0 or > 5 || opponentSuccesses is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(casterPool));
        var rollAudit = ApplyBeastToRoll(DicePoolRules.Roll(casterPool, casterHunger,
            ContestRules.ActingDifficulty(opponentSuccesses), random), beastSubdued);
        var won = ContestRules.ActingCharacterWins(rollAudit.Roll.Successes, opponentSuccesses);
        var proposal = new RitualEffectProposal(won, won
            ? $"O conjurador venceu a disputa para carregar {passenger.Trim()}; movimento e posição continuam sob controle da mesa."
            : $"O conjurador não conseguiu carregar {passenger.Trim()}; nenhuma ficha foi movida ou alterada.", "Esta tentativa");
        return new(state, proposal, rollAudit.Roll,
            BeastSubduedConsidered: rollAudit.BeastSubduedConsidered,
            MessyCriticalConverted: rollAudit.MessyCriticalConverted);
    }

    /// <summary>Resolve an identified opponent's attempt to pull the airborne caster to the ground.</summary>
    public RitualWorkflowResolution ResolveAirGroundingContest(RitualUseDefinition ritual, RitualWorkflowState state,
        string opponent, int opponentPool, int opponentHunger, int casterSuccesses, bool confirmed,
        Random? random = null, bool beastSubdued = false)
    {
        if (ritual.Kind != "air" || state is not { Stage: "effect", Resolved: true } ||
            string.IsNullOrWhiteSpace(opponent) || !confirmed)
            throw new InvalidOperationException("Selecione um consumo ativo de Essência do Ar e confirme quem tenta puxar o conjurador e a oposição com o Narrador.");
        if (opponentPool is < 0 or > 50 || opponentHunger is < 0 or > 5 || casterSuccesses is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(opponentPool));
        var rollAudit = ApplyBeastToRoll(DicePoolRules.Roll(opponentPool, opponentHunger,
            ContestRules.ActingDifficulty(casterSuccesses), random), beastSubdued);
        var won = ContestRules.ActingCharacterWins(rollAudit.Roll.Successes, casterSuccesses);
        var proposal = new RitualEffectProposal(won, won
            ? $"{opponent.Trim()} venceu a disputa para puxar o conjurador ao solo; movimento e posição aguardam aplicação confirmada pela mesa."
            : $"{opponent.Trim()} não conseguiu puxar o conjurador ao solo; nenhuma posição ou ficha foi alterada.", "Esta tentativa");
        return new(state, proposal, rollAudit.Roll,
            BeastSubduedConsidered: rollAudit.BeastSubduedConsidered,
            MessyCriticalConverted: rollAudit.MessyCriticalConverted);
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
        if (entry.Active && RitualUseCatalog.Find(entry.PowerId)?.Kind == "ward")
            return "Use o encerramento da proteção de objeto: confirme com a mesa que o objeto foi fisicamente destruído ou quebrado.";
        if (entry.Active && RitualUseCatalog.Find(entry.PowerId)?.Kind == "circle" &&
            entry.RitualWorkflow is { Stage: "circle-ready", Resolved: true })
            return "Use a expiração do Círculo de Proteção: confirme o amanhecer ou valide um ano e um dia pela data da crônica.";
        if (entry.PowerId == "ritual-essence-of-air" && entry.RitualWorkflow is { Stage: "potion", Charges: > 0 })
            return "Use a expiração de Essência do Ar: informe a noite da crônica concluída e confirme o descarte das doses não consumidas.";
        if (entry.PowerId == "ritual-incorporeal-passage" && entry.RitualWorkflow is { Stage: "effect", Resolved: true })
            return "Use o encerramento de Passagem Incorpórea: confirme se a cena terminou ou o caco foi largado e onde ocorreu a materialização.";
        if (entry.PowerId == "ritual-defense-of-sacred-haven" && entry.RitualWorkflow is { Stage: "effect", Resolved: true })
            return "Use o encerramento de Defesa do Refúgio Sagrado: confirme o fim do dia protegido ou a saída do conjurador da área.";
        if (entry.PowerId == "ritual-deflection-of-wooden-doom" && entry.RitualWorkflow is { Resolved: false })
            return "Resolva a primeira tentativa genuína de estaquear ou use o encerramento da Deflecção para confirmar o fim da noite ou a remoção da lasca.";
        if (entry.PowerId == "ritual-firewalker" && entry.RitualWorkflow is { Stage: "effect", Resolved: true })
            return "Use o encerramento de Andarilho do Fogo: informe a noite da crônica e confirme que ela terminou.";
        if (entry.PowerId == "ritual-clinging-of-the-insect" && entry.RitualWorkflow is { Stage: "effect", Resolved: true })
            return "Use o encerramento de Aderência do Inseto: confirme o fim da cena ou, em vitória crítica, o fim da noite.";
        if (entry.PowerId == "ritual-communicate-with-kindred-sire" && entry.RitualWorkflow is { Stage: "effect", Resolved: true })
            return "Use o encerramento da Comunicação com o Senhor: confirme os dez minutos de comunicação ou uma perturbação em qualquer lado.";
        if (entry.PowerId == "ritual-truth-of-blood" && entry.RitualWorkflow is { Stage: "statements", Resolved: false })
            return "Use o encerramento de Verdade do Sangue: confirme o fim da cena; uma falha na disputa já encerra o ritual automaticamente.";
        if (entry.PowerId == "ritual-dagons-call" && entry.RitualWorkflow is { Stage: "repeat", Resolved: false })
            return "Resolva a próxima repetição paga de O Chamado de Dagon ou confirme que o conjurador encerrou o preparo.";
        if (entry.PowerId == "ritual-eyes-of-babel" && entry.RitualWorkflow is { Stage: "effect", Resolved: true })
            return "Use a expiração de Olhos de Babel: informe e confirme a data atual da crônica para validar uma semana ou, em vitória crítica, um mês.";
        if (entry.PowerId == "ritual-illuminate-trail-of-prey" && entry.RitualWorkflow is { Stage: "effect", Resolved: true })
            return "Use a expiração de Iluminar o Rastro da Presa: confirme o fim da noite ou, em vitória crítica, da segunda noite.";
        if (entry.PowerId == "ritual-craft-bloodstone" && entry.RitualWorkflow is { Stage: "effect", Resolved: true })
            return "Use o encerramento da Pedra de Sangue: confirme sua destruição ou informe a data da crônica para validar uma semana.";
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

    public RitualWorkflowResolution RemoveShaftSplinterByAmputation(RitualWorkflowState state,
        bool doctorUnavailableConfirmed, bool amputationConfirmed, bool beforeTrunkConfirmed)
    {
        if (state.Stage != "splinter")
            throw new InvalidOperationException("Selecione uma lasca ativa do Fuste antes de registrar a amputação.");
        if (!doctorUnavailableConfirmed)
            throw new InvalidOperationException("Confirme com a mesa que não há médico disponível para extrair a lasca.");
        if (!beforeTrunkConfirmed)
            throw new InvalidOperationException("Confirme com a mesa que a lasca ainda não alcançou o tronco.");
        if (!amputationConfirmed)
            throw new InvalidOperationException("Confirme com a mesa que o membro afetado foi amputado para remover a lasca.");
        return new(state with { Stage = "removed" },
            new(true, "Sem médico disponível, a amputação foi confirmada antes de a lasca alcançar o tronco; a lasca foi removida. Nenhuma ficha externa foi alterada.",
                "Encerrado"), null);
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
public enum SacredHavenEnding { DayEnded, CasterLeftArea }
public enum DeflectionEnding { NightEnded, SplinterRemoved }
public enum ClingingEnding { SceneEnded, NightEnded }
public enum BloodstoneEnding { WeekElapsed, Destroyed }
internal sealed record RitualRollAudit(DicePoolResult Roll, bool BeastSubduedConsidered,
    bool MessyCriticalConverted);
