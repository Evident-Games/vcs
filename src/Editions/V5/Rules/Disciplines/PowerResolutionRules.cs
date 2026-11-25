using Game.Content;
using Game.Content.Disciplines;
using Game.Rules.Disciplines;
using Game.Rules.V5.Dice;

namespace Game.Rules.V5.Disciplines;

[RuleSource("Vampiro: A Máscara V5 (Livro base), p. 121 e pp. 244–274")]
public sealed class V5PowerRuleset : IPowerRuleset
{
    public string EditionId => "V5";

    public static PowerResolution Resolve(PowerUseDefinition use, PowerResolutionContext context)
    {
        if (context.Rating is < 0 or > 5 || context.Successes < 0 || context.Difficulty < 0 ||
            context.OpponentSuccesses < 0 || context.RouseChecks is < 0 or > 10 || context.Wits is < 0 or > 5 ||
            context.BloodPotency is < 0 or > 10)
            throw new ArgumentOutOfRangeException(nameof(context));
        var opposed = use.Mode == "opposed";
        var margin = context.Rolled ? context.Successes - (opposed ? context.OpponentSuccesses : context.Difficulty) : 0;
        var won = !context.Rolled || (opposed
            ? ContestRules.ActingCharacterWins(context.Successes, context.OpponentSuccesses)
            : margin >= 0);
        var critical = context.Rolled && won && context.Critical;
        var rating = context.Rating;
        var vampire = context.VampireTarget;
        var result = new PowerResolution(won, use.Conditions, use.Duration, Math.Max(0, margin));
        if (!won)
        {
            var failure = use.Operation switch
            {
                "frenzyTransfer" => "Transferência falhou: o usuário entra no frenesi original.",
                "dread" when !vampire => "Mortal recua e só pode se defender por um turno, mesmo com a falha do usuário.",
                "entrancement" => "Transe falhou. Se era renovação, o alvo fica imune pelo resto da noite.",
                "impostor" => "Disfarce pouco convincente; conhecidos da pessoa imitada percebem a diferença.",
                "command" or "memory" or "memoryScenes" or "possessionHuman" when context.Successes == 0 =>
                    "Falha total: novas tentativas de Dominação/Possessão contra este alvo ficam bloqueadas pela história.",
                _ => "Efeito não obtido. Custos de uma tentativa válida continuam pagos."
            };
            return result with { Effect = failure };
        }
        return use.Operation switch
        {
            "senseBeast" => result with { Effect = critical
                ? "Revelar hostilidade, natureza exata, Fome e Ressonância pelo Narrador."
                : "Revelar hostilidade e presença de Besta sobrenatural; detalhes exatos não são automáticos." },
            "swarm" => result with { HealthBonus = 5, Effect = "Perfil do enxame: Saúde 5; defesa 8 dados; distração −2. Intimidação +1 a +3 depende da mesa." },
            "quell" => result with { Turns = vampire ? margin + 1 : 0,
                Duration = vampire ? $"{margin + 1} turno(s)" : "Cena",
                Effect = vampire ? "Besta subjugada: sem Surto de Sangue nem críticos bestiais." + (critical ? " Frenesi encerrado." : "")
                    : "Apatia: alvo só age para preservar a própria vida." },
            "possessionAnimal" => result with { Duration = critical ? "Indefinida, até interrupção" : "Cena", Effect = "Animal possuído; corpo original em transe. Morte do animal causa 1 agravado de Vontade ao usuário." },
            "questions" => result with { Effect = $"Margem {Math.Max(0, margin)}; sucessos {context.Successes}. O Narrador fornece as informações segundo o poder, sem inventar respostas." + (critical ? " Vitória crítica permite revelação adicional quando prevista." : "") },
            "telepathy" => result with { Effect = critical ? "Panorama coerente de pensamentos e intenções; profundidade definida pelo Narrador." : "Pensamentos superficiais; margem permite aprofundar a leitura, conforme o Narrador." },
            "blink" => result with { Effect = "Deslocamento reto de até 50 m, preservando uma ação; resolver terreno perigoso ou disputa antes de concluir." },
            "traversal" => result with { Effect = $"Travessia bem-sucedida, margem {Math.Max(0, margin)}. O alcance final é o combinado antes da tentativa." },
            "draught" => result with { PoolBonus = rating / 2, Effect = $"Concede {rating / 2} pontos temporários na Disciplina e poderes elegíveis conhecidos pelo doador até esse nível; não é bônus cumulativo. Registrar também o gole para Laço." },
            "unerring" or "lightning" => result with { Effect = "Próximo ataque elegível: dificuldade 1, sem defesa. Não rolado ainda. Celeridade 5 e checagem do defensor cancelam essa vantagem." },
            "memoryScenes" => result with { Effect = $"Pode alterar {Math.Max(0, margin)} memória(s) de cena." + (critical ? " Lembranças convincentes." : " Lembranças vagas, questionáveis sob investigação.") },
            "dementation" => result with { WillpowerDamage = margin, Effect = $"{margin} dano(s) superficial(is) de Vontade, antes da redução normal. Se a trilha ficar debilitada, aplicar colapso/Compulsão conforme a natureza do alvo." },
            "resilience" => result with { HealthBonus = rating, Effect = $"+{rating} à capacidade de Saúde, já calculado para poderes conhecidos na ficha." },
            "enduring" => result with { HealthBonus = rating, Effect = $"+{rating} Saúde por animal; até {Math.Max(0, margin)} animal(is) pela margem, ou o familiar automaticamente. Retirar caixas vazias primeiro no término." },
            "defy" => result with { Effect = $"Reserva de conversão: {rating} agravado(s) por cena/ativação. O superficial convertido não pode ser curado até o fim da cena." },
            "facade" => result with { Effect = $"Sondagem: +{(rating + 1) / 2} dificuldade, ou +{rating} dados na resistência quando permitida; nunca ambos." },
            "ghost" => result with { PoolBonus = 3, Effect = "+3 em contornar vigilância automatizada; identificação em gravações tem dificuldade +3. Transmissão ao vivo segue o poder de Ofuscação ativo." },
            "vanish" => result with { Effect = vampire ? "Usuário desaparece; a memória do vampiro permanece." : critical ? "Usuário desaparece e é apagado da memória do mortal." : "Usuário desaparece; memória do mortal fica imprecisa." },
            "conceal" => result with { Duration = $"{ObfuscateOngoingRules.ConcealmentDurationNights(margin)} noite(s)", Effect = "Objeto e ocupantes ocultos para observadores externos; interação inevitável ou detecção pode revelar." },
            "impostor" => result with { Effect = critical ? "Disfarce perfeito, inclusive voz e gestos, sem teste adicional." : "Aparência convincente; voz e gestos ainda exigem Manipulação + Performance." },
            "gathering" => result with { Effect = $"Até {context.Wits + Math.Max(0, context.RouseChecks - 1)} alvo(s) adicional(is), além do usuário. Custos do poder base são separados; revelar o usuário revela o grupo." },
            "leap" => result with { Effect = $"Alcance vertical {rating * 3} m; horizontal {rating * 5} m, sem impulso." },
            "brutal" => result with { Effect = $"Após acertar mordida: até {(vampire ? rating / 2 : rating)} ação(ões) de alimentação. Cada Fome efetivamente saciada causa 1 {(vampire ? "superficial" : "agravado")}; aplicar saciedade e dano real, não usar este limite como saciedade automática." },
            "rage" => result with { Effect = vampire ? "Alvo faz teste de frenesi de fúria, dificuldade 3; não entra automaticamente." : $"+{rating} dados para incitar violência, quando aplicável." },
            "kiss" => result with { PoolBonus = (rating + 1) / 2, Effect = $"Alvo: +{(rating + 1) / 2} a um Atributo Social por uma noite; depois −{(rating + 1) / 2} nas ações que não busquem nova dose por {rating} noite(s). Uma Vontade suspende abstinência por cena." },
            "dread" => result with { Turns = 1, Effect = vampire
                ? $"Apenas defesa por um turno, salvo gasto de {Math.Max(1, margin)} Vontade." + (critical ? " Também testar frenesi de terror, dificuldade 3." : "")
                : critical ? "Mortal congela ou se encolhe em terror." : "Mortal aterrorizado." },
            "entrancement" => result with { PoolBonus = rating, Duration = $"{1 + Math.Max(0, margin)} hora(s)", Effect = $"+{rating} nas paradas sociais contra este alvo; sem acumular com Fascínio. Acompanhar limites dos pedidos." },
            "summon" => result with { Effect = critical ? "Alvo deve vir o mais rápido possível sem perigo imediato." : "Alvo ouve a convocação e reconhece origem/local, mas pode recusar." },
            "majesty" => result with { Effect = "Majestade ativa: resolver cada tentativa de oposição separadamente. Resistência vencedora dá 1 + margem turnos livres; crítico dá uma cena." },
            "form" => result with { Turns = 1, Effect = "Transformação em um turno, sem outras ações. Depois, aplicar o perfil físico confirmado; capacidades naturais e uso de Disciplinas dependem da forma." },
            "mist" => result with { Turns = Math.Max(1, 4 - context.RouseChecks), Effect = $"Transformação em {Math.Max(1, 4 - context.RouseChecks)} turno(s); sem fala, toque ou contato visual. Só fogo, sol e ataques sobrenaturais imateriais ferem." },
            "heart" => result with { Effect = "Estaca expulsa. Não repetir tentativa antes de uma hora; proteção passiva de Coração Vagante permanece." },
            "taste" => result with { Effect = critical ? "Revelar natureza, Ressonância ou Potência relativa, além de diablerie e detalhes permitidos pelo livro. Não revela automaticamente o Clã." : "Revelar natureza do sangue, Ressonância/intensidade humana ou Potência relativa vampírica." },
            "corrosion" => result with { Effect = $"{context.RouseChecks} dose(s) aplicadas ao objeto. Resistência do material, extensão e tempo devem ser confirmados; não afeta carne viva ou vampírica." },
            "extinguish" => result with { HungerIncrease = critical ? 2 : 1, Effect = $"Fome do alvo vampiro aumenta em {(critical ? 2 : 1)}, limitada a 5." },
            "scorpion" => result with { HealthDamage = margin, Aggravated = !vampire, Effect = vampire ? $"{margin} superficial(is) sem divisão pela metade." : $"{margin} agravado(s); mortal ferido fica inconsciente." },
            "potency" => result with { PotencyIncrease = critical ? 2 : 1, Effect = $"Potência temporária +{(critical ? 2 : 1)} (máximo total 10). Não acumular nem alterar valor permanente." },
            "theft" => result with { Effect = $"Alimentação à distância em mortal à velocidade ×{(critical ? 3 : 2)}. Aplicar saciedade no controle de alimentação conforme quantidade retirada; não há cura automática." },
            "baal" => result with { HealthDamage = margin, Aggravated = true, Effect = vampire ? $"{margin} agravado(s). Segunda disputa necessária para causar Torpor no próximo sono." : $"{margin} agravado(s); mortal ferido morre." },
            "cauldron" => result with { HealthDamage = margin, Aggravated = true, HungerIncrease = vampire ? margin : 0, Effect = vampire ? $"{margin} agravado(s), e Fome +{margin} até 5. Registrar Máculas do usuário." : "Mortal ferido morre; registrar as Máculas determinadas pelo Narrador." },
            _ => result
        };
    }

    PowerResolution IPowerRuleset.Resolve(PowerUseDefinition use, PowerResolutionContext context) => Resolve(use, context);
}
