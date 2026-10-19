using VCS.Domain.Characters;

namespace VCS.Infrastructure.Data;

internal static class ProvidedAdvantages
{
    public static IReadOnlyList<AdvantageDefinition> All { get; } =
    [
        Entry("visual-belo", "Visual", "Visual Belo", false, 2, 2, "2", "Adiciona +1 dado em todos os testes Sociais do personagem."),
        Entry("deslumbrante", "Visual", "Deslumbrante", false, 4, 4, "4", "Adiciona +2 dados em todos os testes Sociais do personagem."),
        Entry("feio", "Visual", "Feio", true, 1, 1, "1", "Subtrai 1 dado em todos os testes Sociais."),
        Entry("repulsivo", "Visual", "Repulsivo", true, 2, 2, "2", "Subtrai 2 dados em todos os testes Sociais."),
        Entry("refugio", "Refúgio", "Refúgio", false, 1, 3, "1 a 3", "Fornece ao seu personagem um lugar seguro."),
        Entry("arsenal-oculto", "Refúgio", "Arsenal oculto", false, 1, 1, "1", "Adiciona armas de fogo extras ao seu refúgio."),
        Entry("vigilancia", "Refúgio", "Vigilância", false, 1, 1, "1", "Fornece seguranças humanos para o refúgio."),
        Entry("luxo", "Refúgio", "Luxo", false, 1, 1, "1", "Garante +2 dados em testes sociais que sejam realizados no interior do refúgio."),
        Entry("nenhum-refugio", "Refúgio", "Nenhum Refúgio", true, 1, 1, "1", "O personagem não possui um local seguro fixo."),
        Entry("assombrado", "Refúgio", "Assombrado", true, 1, 1, "1", "O refúgio possui uma presença fantasmagórica."),
        Entry("comprometido", "Refúgio", "Comprometido", true, 2, 2, "2", "O refúgio está sob vigilância de outras pessoas."),
        Entry("recursos", "Recursos", "Recursos", false, 1, 5, "1 a 5", "Representa o seu fluxo de caixa e dinheiro disponível."),
        Entry("destituido", "Recursos", "Destituído", true, 1, 1, "1", "O personagem não tem dinheiro e nem emprego."),
        Entry("sabujo-de-sangue", "Alimentação", "Sabujo de Sangue", false, 1, 1, "1", "O personagem é capaz de farejar a Ressonância do sangue humano."),
        Entry("esofago-de-ferro", "Alimentação", "Esôfago de Ferro", false, 3, 3, "3", "O vampiro é capaz de consumir sangue rançoso ou fracionado sem problemas."),
        Entry("exclusao-de-presa", "Alimentação", "Exclusão de presa", true, 1, 1, "1", "Torna o personagem incapaz de se alimentar de um determinado grupo específico de pessoas."),
        Entry("sede-do-matusalem", "Alimentação", "Sede do Matusalém", true, 1, 1, "1", "O personagem é incapaz de se saciar completamente apenas bebendo sangue humano."),
        Entry("fazendeiro", "Alimentação", "Fazendeiro", true, 2, 2, "2", "Exige que você gaste 2 pontos de Força de Vontade toda vez que quiser beber sangue que não seja de animais."),
        Entry("organivoro", "Alimentação", "Organívoro", true, 2, 2, "2", "O vampiro só consegue aplacar a Fome consumindo carne ou órgãos humanos."),
        Entry("mawla", "Membros", "Mawla", false, 1, 5, "1 a 5", "O personagem possui um mentor não-vivo que o auxilia."),
        Entry("status", "Membros", "Status", false, 1, 5, "1 a 5", "Reflete a sua reputação dentro da Seita."),
        Entry("adversario", "Membros", "Adversário", true, 1, 5, "1 a 5", "O personagem possui um inimigo não-vivo atuando contra ele."),
        Entry("suspeito", "Membros", "Suspeito", true, 1, 1, "1", "O personagem tem má reputação na seita, sofrendo uma penalidade de -2 dados em testes Sociais com a facção ofendida."),
        Entry("segregado", "Membros", "Segregado", true, 2, 2, "2", "O personagem é totalmente desprezado por uma determinada seita."),
        Entry("vivendo-no-passado", "Acompanhando o Tempo", "Vivendo no Passado", true, 1, 1, "1", "O personagem possui visões e opiniões ultrapassadas."),
        Entry("arcaico", "Acompanhando o Tempo", "Arcaico", true, 2, 2, "2", "A habilidade tecnológica do personagem é fixada em zero."),
        Entry("ingerir-comida", "Mítico", "Ingerir Comida", false, 2, 2, "2", "Permite que o vampiro possa consumir comida humana normal."),
        Entry("perdicao-folclorica", "Mítico", "Perdição Folclórica", true, 1, 1, "1", "O vampiro toma dano real quando toca um item específico de lendas (como prata, cruz sagrada ou alho)."),
        Entry("tabu-folclorico", "Mítico", "Tabu Folclórico", true, 1, 1, "1", "Você deve gastar Força de Vontade para atravessar coisas específicas das lendas, como entrar numa casa sem convite ou cruzar água corrente."),
        Entry("estigma", "Mítico", "Estigma", true, 1, 1, "1", "O vampiro sangra visivelmente pelas mãos, pés e testa sempre que atinge o nível 4 de Fome."),
        Entry("isca-de-estaca", "Mítico", "Isca de Estaca", true, 2, 2, "2", "O personagem encontra a Morte Final (morre de vez) caso seja estacado no coração, em vez de apenas entrar em torpor."),
        Entry("mascara", "Máscara", "Máscara", false, 1, 2, "1 a 2", "Fornece uma identidade falsa confiável no mundo mortal."),
        Entry("cadaver-identificado", "Máscara", "Cadáver Identificado", true, 1, 1, "1", "O personagem é sabidamente e publicamente dado como morto."),
        Entry("corpo-frio-identificado", "Máscara", "Corpo Frio Identificado", true, 2, 2, "2", "O personagem está registrado em Órgãos Governamentais como sendo uma criatura vampírica."),
        Entry("linguistica", "Linguística", "Linguística", false, 1, 1, "1", "Cada ponto comprado representa um novo idioma no qual você é fluente, além do seu nativo."),
        Entry("analfabeto", "Linguística", "Analfabeto", true, 1, 1, "1", "O personagem não sabe ler ou escrever em nenhum idioma."),
        Entry("lacaios", "Mortais", "Lacaios", false, 1, 3, "1 a 3", "Concede leais servos mortais ao personagem."),
        Entry("aliados", "Mortais", "Aliados", false, 1, 5, "1 a 5", "Representa um grupo de suporte mortal; os pontos indicam a Efetividade ou Confiabilidade deles."),
        Entry("contatos", "Mortais", "Contatos", false, 1, 3, "1 a 3", "Mortais que não lutam, mas fornecem informações, itens ou coisas de valor para você."),
        Entry("rebanho", "Mortais", "Rebanho", false, 1, 5, "1 a 5", "Um grupo de mortais de onde você pode se alimentar sem preocupações, mas eles são menos leais que Lacaios."),
        Entry("perseguidores", "Mortais", "Perseguidores", true, 1, 1, "1", "O personagem tem seguidores mortais indesejados."),
        Entry("inimigo", "Mortais", "Inimigo", true, 1, 5, "1+", "O exato oposto dos Aliados; inimigos mortais com grande Efetividade contra você."),
        Entry("predador-manifesto", "Mortais", "Predador Manifesto", true, 2, 2, "2", "O vampiro assusta e repele os mortais ao seu redor, sendo incapaz de manter um Rebanho.")
    ];

    private static AdvantageDefinition Entry(string id, string group, string name, bool flaw, int minimum, int maximum,
        string points, string description) => new()
        {
            Id = "catalog-" + id, Group = group, Name = name, Category = flaw ? TraitCategory.Flaw : TraitCategory.Advantage,
            Minimum = minimum, Maximum = maximum, PointsLabel = points, Description = description
        };
}
