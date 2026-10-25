using VCS.Domain.Advantages;
using VCS.Domain.Characters;

namespace VCS.Infrastructure.Catalogs.Seed;

public static class ProvidedLoresheets
{
    public static IReadOnlyList<AdvantageDefinition> All { get; } =
    [
        Benefit("bahari", "Os Bahari", 1, "Reputação Perigosa",
            "Uma vez por história, concede dois dados extras em testes de Intimidação contra adoradores de Caim que conheçam sua afiliação."),
        Benefit("bahari", "Os Bahari", 2, "Escarificação Ritual",
            "Uma vez por sessão, permite que você cause dano Agravado em si mesmo (escarificação) para recuperar um nível de dano (Superficial ou Agravado) na Força de Vontade."),
        Benefit("bahari", "Os Bahari", 3, "Sacrifício dos Filhos",
            "Cometer Diablerie em sua própria Criança concede três dados extras na parada de Humanidade + Potência de Sangue para absorver Disciplinas."),
        Benefit("bahari", "Os Bahari", 4, "Sangue do Útero",
            "Uma vez por história, beber sangue do útero concede dois pontos temporários em Vigor ou Determinação (aumentando Vitalidade/Força de Vontade) até o amanhecer."),
        Benefit("bahari", "Os Bahari", 5, "Amaldiçoada Original",
            "Permite andar na primeira/última hora de sol e fazer sexo sem Inflamar o Sangue, mas você atrai o defeito Predador Manifesto, falha mais fácil em testes de difamação e causa enxaqueca em quem usar Auspícios em você."),
        Benefit("theo-bell", "Theo Bell", 1, "Célula Rebelde",
            "Fornece um grupo mortal rebelde (equivalente a Aliado de 3 pontos) que fará uma missão perigosa sozinha para você antes de debandar até a próxima história."),
        Benefit("theo-bell", "Theo Bell", 2, "Anarch Verdadeiro",
            "Garante dois sucessos automáticos em qualquer teste investigativo envolvendo vampiros que desertaram da Camarilla para o Movimento Anarch."),
        Benefit("theo-bell", "Theo Bell", 3, "Informação de Contato",
            "Permite contatar Theo Bell para enviar uma mensagem; os efeitos e a resposta dependem do Narrador e da importância do recado."),
        Benefit("theo-bell", "Theo Bell", 4, "Círculo de Bell",
            "Transforma Theo Bell em um Mawla de 5 pontos, trazendo conselhos valiosos, mas também complicações e perigos devido à associação com ele."),
        Benefit("theo-bell", "Theo Bell", 5, "Neutralidade de Seita",
            "Concede seguidores Brujah leais e cinco pontos para distribuir livremente entre Contatos, Refúgio, Mawla e Lacaios até que eles decidam se rebelar."),
        Benefit("heresia-cainita", "Heresia Cainita", 1, "Que Ele tenha Compreensão",
            "Uma vez por história, o Narrador é obrigado a fornecer uma pista grátis para detectar ou entender os planos da Heresia."),
        Benefit("heresia-cainita", "Heresia Cainita", 2, "Mão da Heresia",
            "Garante três pontos para distribuir entre Aliados, Rebanho, Mawla ou Lacaios no culto, mas aplica o defeito Segredo Sombrio (Heresia)."),
        Benefit("heresia-cainita", "Heresia Cainita", 3, "Contra-Inquisição",
            "Permite usar Determinação + Heresia Cainita para detectar passivamente a Fé Verdadeira em mortais ou saber em quem confiar."),
        Benefit("heresia-cainita", "Heresia Cainita", 4, "Celebrante Vermelho",
            "Uma vez por história, induz humanos a um frenesi extático (Pentecostes Vermelho) com ervas, custando uma Checagem de Sangue e podendo causar frenesi em Membros."),
        Benefit("heresia-cainita", "Heresia Cainita", 5, "Nomeado na Profecia",
            "Uma vez por história, devido à sua importância nas profecias, você pode ditar o vencedor de um conflito Social fornecendo uma desculpa plausível."),
        Benefit("carna", "Carna", 1, "Abraçar a Visão",
            "O conforto entre colegas de subfacção lhe garante um dado adicional em todos os testes de Força de Vontade perto deles."),
        Benefit("carna", "Carna", 2, "O Rastro Rebelde",
            "Permite rolar Força de Vontade (Dificuldade igual à Potência do Sangue ingerido) para ignorar completamente o risco de criar um Laço de Sangue."),
        Benefit("carna", "Carna", 3, "Rituais Heterodoxos",
            "Uma vez por história, realiza um ritual sem Checagem de Sangue, mas um Crítico Bestial causa um trauma/paranoia temporária até o fim da história."),
        Benefit("carna", "Carna", 4, "Laço Reimaginado",
            "Transar com um alvo contorna a perdição Tremere e cria um Laço de Sangue temporário (até o fim da história) entre você, o alvo e Carna."),
        Benefit("carna", "Carna", 5, "Livro da Guerra dos Túmulos",
            "Imuniza contra Laços de Sangue e dá sucesso automático em testes de Ocultismo sobre a Gehenna, mas a paranoia custa 1 dado em testes Sociais/Mentais.")
    ];

    private static AdvantageDefinition Benefit(string key, string collection, int points, string name, string description) =>
        new()
        {
            Id = $"lore-{key}-{points}", Group = "Ficha de Conhecimento", LoresheetCollection = collection,
            Name = name, Description = description, Category = TraitCategory.Loresheet,
            Minimum = points, Maximum = points, PointsLabel = points.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
}
