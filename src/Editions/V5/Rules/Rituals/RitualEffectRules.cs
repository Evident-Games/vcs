using Game.Content;
using Game.Content.Disciplines;
using Game.Rules.Rituals;
using Game.Rules.V5.Dice;

namespace Game.Rules.V5.Rituals;

/// <summary>Core ritual effects. World facts and changes to targets remain explicit table decisions.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), p. 121 e pp. 275–282")]
public static class RitualEffectRules
{
    public static RitualEffectProposal Resolve(RitualUseDefinition ritual, RitualRollContext roll,
        int opponentSuccesses = 0, bool vampireTarget = true)
    {
        var opposed = ritual.Kind is "truth" or "dagon";
        var margin = opposed ? roll.Successes - opponentSuccesses : roll.Successes - roll.Difficulty;
        var success = opposed
            ? ContestRules.ActingCharacterWins(roll.Successes, opponentSuccesses)
            : margin >= 0;
        if (!success) return new(false, "O teste falhou; nenhum benefício do ritual foi concedido.", "Encerrado");
        var critical = roll.Critical;
        return ritual.Kind switch
        {
            "clinging" => new(true, "Pode aderir a paredes e tetos com mãos e pés, a cerca de metade da velocidade habitual; o benefício é do conjurador.", critical ? "Até o fim da noite" : "Cena"),
            "blood-walk" => new(true, "O Narrador informa geração e nome do alvo e do Senhor." + (critical ? " Informe também os Laços de Sangue ativos, como regente ou escravo." : ""), "Informação obtida"),
            "bloodstone" => new(true, "Pedra sintonizada: o conjurador percebe direção e distância. O limite de pedras é sua Determinação; destruição encerra o vínculo.", "Uma semana ou até a pedra ser destruída"),
            "sire" => new(true, critical ? "Comunicação silenciosa com o Senhor disponível imediatamente, enquanto não houver perturbação." : "Comunicação silenciosa com o Senhor disponível após mais quinze minutos, enquanto não houver perturbação.", "Dez minutos de comunicação"),
            "trail" => new(true, $"Para seguir o rastro, use Inteligência + Sobrevivência; dificuldade calculada {Math.Max(0, 6 - margin)}. O Narrador fornece o caminho das últimas 24 horas.", critical ? "Duas noites" : "Até o fim da noite", FollowDifficulty: Math.Max(0, 6 - margin)),
            "babel" => new(true, "Concede leitura e fala dos idiomas conhecidos pela vítima; a mesa informa quais são.", critical ? "Um mês" : "Uma semana"),
            "truth" => new(true, "O Narrador informa se a afirmação é verdadeira conforme o conhecimento do alvo. Não recupera memórias apagadas." + (critical ? " A resposta se amplia com detalhes involuntários." : ""), "Até falha na disputa ou fim da cena"),
            "firewalker" => new(true, "O beneficiário sofre metade do dano de fogo pelo restante da noite. As pontas dos dedos sacrificadas devem ser do conjurador, mesmo quando beneficiar terceiros.", "Até o fim da noite"),
            "dagon" => new(true, $"Dano proposto: {margin} ponto(s) {(vampireTarget ? "superficiais a vampiro" : "agravados a mortal")}. Aplique as regras da trilha do alvo. Este preparo admite até duas repetições, cada uma com nova Checagem de Sangue.", "Execução imediata", margin, !vampireTarget),
            "deflection" => new(true, "A primeira estaca genuinamente usada para estaquear se despedaça." + (critical ? " O atacante também fica cego por dois turnos." : ""), "Primeira tentativa resolvida"),
            "air" => new(true, "Poção preparada; o consumo posterior só beneficia o conjurador. Descarte doses não consumidas ao fim da noite.", "Até o fim da noite", Charges: critical ? 2 : 1),
            "haven" => new(true, "A área protegida impede dano por luz solar; o conjurador deve ter permanecido até o nascer do sol." + (critical ? " Quem está dentro pode ver vagamente o exterior." : ""), "Um dia ou até o conjurador sair"),
            "nighthawk" => new(true, "Controle do voo e visão da ave carnívora." + (critical ? " A ave pode executar ações simples; Disciplinas exigem seus próprios requisitos." : "") + " Remova seus olhos ao encerrar; caso contrário, o conjurador fica cego pelas próximas três noites.", "Até o fim da noite"),
            "incorporeal" => new(true, "Incorpóreo enquanto segura o caco: sem interação física; atravessar matéria sólida exige linha reta. Fogo, sol e efeitos arcanos adequados ainda afetam. Não pode Inflamar o Sangue; reaparecer em matéria sólida exige decisão do Narrador.", "Cena ou até largar o caco"),
            "heart-stone" => new(true, "Coração imune a estacas; −3 dados em Remorso e testes sociais ativos, exceto Intimidação e Dominação. Não usa Presença; +3 dados para resistir a Presença. Repetir o ritual reverte o efeito." + (critical ? " A vitória crítica cura o ponto agravado causado pela vela; confirme essa cura específica." : ""), "Indefinido, até reverter o ritual"),
            "shaft" => new(true, "Estaca preparada: +3 dados para estaquear. Margem de ataque de pelo menos cinco causa Morte Final; acertos de margem menor iniciam o avanço da lasca. Remoção cirúrgica: Destreza + Medicina, dificuldade 6, até quatro horas. O Narrador confirma ataque e alvo; nenhuma morte é aplicada automaticamente.", "Até a arma ser usada ou inutilizada", PoolBonus: 3),
            _ => throw new InvalidOperationException("Este procedimento tem um resolvedor próprio; não use o teste genérico.")
        };
    }

    public static RitualEffectProposal CircleContact(int casterSuccesses, bool critical, int intruderSuccesses)
    {
        if (casterSuccesses < 0 || intruderSuccesses < 0) throw new ArgumentOutOfRangeException(nameof(casterSuccesses));
        // In a V5 contest, the acting character wins by equalling or exceeding the opponent (p. 121).
        // The intruder is the acting character when attempting to cross the circle (pp. 275–276).
        if (ContestRules.ActingCharacterWins(intruderSuccesses, casterSuccesses))
            return new(false, intruderSuccesses == casterSuccesses
                ? "O intruso igualou os sucessos do círculo e venceu a disputa como personagem ativo; pode atravessar conforme o sentido do círculo."
                : "O intruso venceu a disputa e pode atravessar conforme o sentido do círculo.", "Contato resolvido");
        return new(true, $"Entrada impedida; dano proposto 3 {(critical ? "agravados" : "superficiais")}. Para tentar novamente após ser afetado, o intruso gasta 1 Vontade e repete sua metade da disputa.", "Contato resolvido", 3, critical);
    }
}

public sealed class V5RitualEffectRuleset : IRitualEffectRuleset
{
    public string EditionId => "V5";
    public RitualEffectProposal Resolve(RitualUseDefinition ritual, RitualRollContext roll, int opponentSuccesses, bool vampireTarget) =>
        RitualEffectRules.Resolve(ritual, roll, opponentSuccesses, vampireTarget);
    public RitualEffectProposal CircleContact(int casterSuccesses, bool critical, int intruderSuccesses) =>
        RitualEffectRules.CircleContact(casterSuccesses, critical, intruderSuccesses);
}
