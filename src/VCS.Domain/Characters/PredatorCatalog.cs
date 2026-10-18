namespace VCS.Domain.Characters;

public sealed record PredatorSpecialization(string Skill, string Text, bool IsFreeText = false)
{
    public string Label => $"{Skill} ({Text})";
}
public sealed record PredatorAllocation(string Name, TraitCategory Category);
public sealed record PredatorDefinition(string Id, string Name, string Description, string Benefits, string Cost,
    string[] Disciplines, PredatorSpecialization[] Specializations, PredatorAllocation[] FixedGrants,
    int[] FixedPoints, PredatorAllocation[] AllocationOptions, int AllocationPoints,
    PredatorAllocation[] FlawOptions, int FlawPoints, bool SplitFlaws = false, int HumanityDelta = 0, int BloodDelta = 0);

public static class PredatorCatalog
{
    private static PredatorAllocation Advantage(string name) => new(name, TraitCategory.Advantage);
    private static PredatorAllocation Flaw(string name) => new(name, TraitCategory.Flaw);
    public static IReadOnlyList<PredatorDefinition> All { get; } =
    [
        new("alleycat", "Vira-lata",
            "Um caçador agressivo que ataca suas vítimas nas ruas, seja disfarçando a caçada como um assalto ou dominando sua presa pela força.",
            "+1 Celeridade ou Potência; +3 Contatos Criminosos; especialização em Intimidação (Assalto à Mão Armada) ou Briga (Agarramento).",
            "Perde 1 ponto de Humanidade.",
            ["Celeridade", "Potência"], [new("Intimidação", "Assalto à Mão Armada"), new("Briga", "Agarramento")],
            [Advantage("Contatos Criminosos")], [3], [], 0, [], 0, HumanityDelta: -1),
        new("extortionist", "Chantagista", "Adquire sangue mediante ameaças e extorsão.",
            "+1 Dominação ou Potência; 3 pontos entre Contatos e Recursos; especialização em Intimidação (Coerção) ou Ladroagem (Segurança).",
            "Inimigo 2 (a vítima extorquida ou a polícia).",
            ["Dominação", "Potência"], [new("Intimidação", "Coerção"), new("Ladroagem", "Segurança")],
            [Flaw("Inimigo (extorsao)")], [2], [Advantage("Contatos"), Advantage("Recursos")], 3, [], 0),
        new("roadside", "Assassino na Estrada", "Um viajante focado em caçar pessoas em movimento ou distantes da lei, como mochileiros e motoristas solitários.",
            "+1 Fortitude ou Proteanismo; +2 Rebanho Itinerante; especialização em Sobrevivência (Estradas) ou Investigação (Grafite Vampírico).",
            "Exclusão de Presas 1 (locais sedentários).",
            ["Fortitude", "Proteanismo"], [new("Sobrevivência", "Estradas"), new("Investigação", "Grafite Vampírico")],
            [Advantage("Rebanho Itinerante"), Flaw("Exclusao de Presas (locais sedentarios)")], [2, 1], [], 0, [], 0),
        new("cleaver", "Trinchador", "Um parasita familiar; esconde a sua natureza bestial de seus amigos e familiares mundanos, enquanto se alimenta secretamente deles no convívio diário.",
            "+1 Dominação ou Animalismo; +2 Rebanho; especialização em Persuasão (Gaslighting) ou Subterfúgio (Encobrimento).",
            "Segredo Sombrio 1: Trinchador.",
            ["Dominação", "Animalismo"], [new("Persuasão", "Gaslighting"), new("Subterfúgio", "Encobrimento")],
            [Advantage("Rebanho"), Flaw("Segredo Sombrio (Trinchador)")], [2, 1], [], 0, [], 0),
        new("consensualist", "Consensualista", "Só se alimenta do sangue de pessoas que aceitam e oferecem livremente; pode dizer a verdade ou se disfarçar de trabalhador de caridade/adepto de cena fetiche vampírica.",
            "+1 Auspícios ou Fortitude; +1 Humanidade; especialização em Medicina (Flebotomia) ou Persuasão (Bolsas).",
            "Segredo Sombrio 1: Quebrador da Máscara; Exclusão de Presa 1 (sem consentimento).",
            ["Auspícios", "Fortitude"], [new("Medicina", "Flebotomia"), new("Persuasão", "Bolsas")],
            [Flaw("Segredo Sombrio (Quebrador da Mascara)"), Flaw("Exclusao de Presa (sem consentimento)")], [1, 1], [], 0, [], 0, HumanityDelta: 1),
        new("osiris", "Osíris", "Alguém com poder de multidão (líderes de culto, DJs, celebridades) que bebe o sangue de fãs fanáticos e adoradores em transe.",
            "+1 Feitiçaria de Sangue (só Tremere) ou Presença; 3 pontos em Fama/Rebanho; especialização em Ocultismo (tradição específica) ou Performance (cena específica).",
            "2 pontos entre Defeitos Inimigos ou Defeitos Míticos.",
            ["Feitiçaria de Sangue", "Presença"], [new("Ocultismo", "tradição específica", true), new("Performance", "cena específica", true)],
            [], [], [Advantage("Fama"), Advantage("Rebanho")], 3,
            [Flaw("Inimigo"), Flaw("Perdicao Folclorica"), Flaw("Tabu Folclorico"), Flaw("Estigma"), Flaw("Isca de Estaca")], 2, SplitFlaws: true),
        new("scene-queen", "Scene Queen", "Um socialite que usa seu prestígio dentro de uma subcultura específica para encantar as vítimas como um colega influente da mesma cena.",
            "+1 Dominação ou Potência; +1 Contatos; +1 Fama; especialização em Etiqueta, Liderança ou Manha (Cena Específica).",
            "Rejeitado 1 OU Exclusão de Presa 1 (Subcultura diferente).",
            ["Dominação", "Potência"], [new("Etiqueta", "Cena Específica", true), new("Liderança", "Cena Específica", true), new("Manha", "Cena Específica", true)],
            [Advantage("Contatos"), Advantage("Fama")], [1, 1], [], 0,
            [Flaw("Rejeitado"), Flaw("Exclusao de Presa (Subcultura diferente)")], 1),
        new("siren", "Sereia", "O predador focado em luxúria que caça pessoas fingindo desejo sexual, alimentando-se enquanto a vítima está seduzida.",
            "+1 Fortitude ou Presença; Visual Belo 2; especialização em Persuasão (Sedução) ou Subterfúgio (Sedução).",
            "Inimigo 1 (ex-amante abandonado ou parceiro ciumento).",
            ["Fortitude", "Presença"], [new("Persuasão", "Sedução"), new("Subterfúgio", "Sedução")],
            [Advantage("Visual Belo"), Flaw("Inimigo (ex-amante ou parceiro ciumento)")], [2, 1], [], 0, [], 0),
        new("sandman", "Sandman", "Extrema furtividade e discrição noturna; esgueira-se pelas casas e suga sangue de pessoas profundamente adormecidas sem deixar rastro.",
            "+1 Auspícios ou Ofuscação; +1 Recursos; especialização em Medicina (Anestésicos) ou Furtividade (Invasão).",
            "Não há custo negativo.",
            ["Auspícios", "Ofuscação"], [new("Medicina", "Anestésicos"), new("Furtividade", "Invasão")],
            [Advantage("Recursos")], [1], [], 0, [], 0),
        new("graverobber", "Ladrão de Sepultura", "Restringe-se a caçar presas nos cemitérios.",
            "+1 Fortitude ou Oblívio; Esôfago de Ferro 3; Refúgio 1; especialização em Ocultismo (Rituais Tumulares) ou Medicina (texto livre).",
            "Predador Manifesto 2.",
            ["Fortitude", "Oblívio"], [new("Ocultismo", "Rituais Tumulares"), new("Medicina", "especialização a informar", true)],
            [Advantage("Esofago de Ferro"), Advantage("Refugio"), Flaw("Predador Manifesto")], [3, 1, 2], [], 0, [], 0),
        new("bagger", "Sacoleiro", "Evita violência de caça real, adquirindo sangue empacotado em hospitais ou contrabandeando do mercado negro. Ventrue não pode escolher.",
            "+1 Ofuscação, Oblívio ou Feitiçaria de Sangue (só Tremere); Esôfago de Ferro 3; especialização em Ladroagem (Abrir Fechaduras) ou Manha (Mercado Negro).",
            "Inimigo 2 (cobrando dívidas ou banindo-o das ruas).",
            ["Ofuscação", "Oblívio", "Feitiçaria de Sangue"], [new("Ladroagem", "Abrir Fechaduras"), new("Manha", "Mercado Negro")],
            [Advantage("Esofago de Ferro"), Flaw("Inimigo (dividas ou banimento)")], [3, 2], [], 0, [], 0),
        new("blood-leech", "Sanguessuga", "Um canibal de Membros e Diablerista ocasional; detesta se alimentar de humanos e busca sangue de outros vampiros.",
            "+1 Celeridade ou Proteanismo; +1 Potência de Sangue; especialização em Briga (Membros) ou Furtividade (Contra Membros).",
            "-1 Humanidade; Segredo Sombrio 2 (Diablerista) OU Segregado 2; Exclusão de Presa 2 (Mortais).",
            ["Celeridade", "Proteanismo"], [new("Briga", "Membros"), new("Furtividade", "Contra Membros")],
            [Flaw("Exclusao de Presa (Mortais)")], [2], [], 0,
            [Flaw("Segredo Sombrio (Diablerista)"), Flaw("Segregado")], 2, HumanityDelta: -1, BloodDelta: 1),
        new("farmer", "Fazendeiro", "Não ataca seres humanos e foca sua alimentação em animais. Ventrue e Membros com Potência de Sangue 3+ não podem escolher.",
            "+1 Animalismo ou Proteanismo; +1 Humanidade; especialização em Empatia com Animais (escolha um) ou Sobrevivência (Caça).",
            "Defeito de Alimentação 2: Fazendeiro.",
            ["Animalismo", "Proteanismo"], [new("Empatia com Animais", "animal a informar", true), new("Sobrevivência", "Caça")],
            [Flaw("Fazendeiro")], [2], [], 0, [], 0, HumanityDelta: 1)
    ];

    public static PredatorDefinition? Find(string id) => All.FirstOrDefault(x => x.Id == id);
    public static string[] DisciplinesFor(PredatorDefinition definition, string clan) =>
        definition.Disciplines.Where(x => !ClanCatalog.SameName(x, "Feitiçaria de Sangue") ||
            ClanCatalog.Find(clan)?.Name == "Tremere").ToArray();
}
