
using VCS.Content.Advantages;
using VCS.Sheet;

namespace VCS.Infrastructure.Catalogs.Seed;

internal static class ProvidedAdvantages
{
    public static IReadOnlyList<AdvantageDefinition> All { get; } =
    [
        Entry("looks-beautiful", "Visual", "Visual Belo", false, 2, 2, "2", "Adiciona +1 dado em todos os testes Sociais do personagem."),
        Entry("looks-stunning", "Visual", "Deslumbrante", false, 4, 4, "4", "Adiciona +2 dados em todos os testes Sociais do personagem."),
        Entry("looks-ugly", "Visual", "Feio", true, 1, 1, "1", "Subtrai 1 dado em todos os testes Sociais."),
        Entry("looks-repulsive", "Visual", "Repulsivo", true, 2, 2, "2", "Subtrai 2 dados em todos os testes Sociais."),
        Entry("haven", "Refúgio", "Refúgio", false, 1, 3, "1 a 3", "Fornece ao seu personagem um lugar seguro."),
        Entry("haven-hidden-armory", "Refúgio", "Arsenal oculto", false, 1, 1, "1", "Adiciona armas de fogo extras ao seu refúgio."),
        Entry("haven-watchmen", "Refúgio", "Vigilância", false, 1, 1, "1", "Fornece seguranças humanos para o refúgio."),
        Entry("haven-luxury", "Refúgio", "Luxo", false, 1, 1, "1", "Garante +2 dados em testes sociais que sejam realizados no interior do refúgio."),
        Entry("no-haven", "Refúgio", "Nenhum Refúgio", true, 1, 1, "1", "O personagem não possui um local seguro fixo."),
        Entry("haven-haunted", "Refúgio", "Assombrado", true, 1, 1, "1", "O refúgio possui uma presença fantasmagórica."),
        Entry("haven-compromised", "Refúgio", "Comprometido", true, 2, 2, "2", "O refúgio está sob vigilância de outras pessoas."),
        Entry("resources", "Recursos", "Recursos", false, 1, 5, "1 a 5", "Representa o seu fluxo de caixa e dinheiro disponível."),
        Entry("destitute", "Recursos", "Destituído", true, 1, 1, "1", "O personagem não tem dinheiro e nem emprego."),
        Entry("bloodhound", "Alimentação", "Sabujo de Sangue", false, 1, 1, "1", "O personagem é capaz de farejar a Ressonância do sangue humano."),
        Entry("iron-gullet", "Alimentação", "Esôfago de Ferro", false, 3, 3, "3", "O vampiro é capaz de consumir sangue rançoso ou fracionado sem problemas."),
        Entry("prey-exclusion", "Alimentação", "Exclusão de presa", true, 1, 1, "1", "Torna o personagem incapaz de se alimentar de um determinado grupo específico de pessoas."),
        Entry("methuselahs-thirst", "Alimentação", "Sede do Matusalém", true, 1, 1, "1", "O personagem é incapaz de se saciar completamente apenas bebendo sangue humano."),
        Entry("farmer", "Alimentação", "Fazendeiro", true, 2, 2, "2", "Exige que você gaste 2 pontos de Força de Vontade toda vez que quiser beber sangue que não seja de animais."),
        Entry("organovore", "Alimentação", "Organívoro", true, 2, 2, "2", "O vampiro só consegue aplacar a Fome consumindo carne ou órgãos humanos."),
        Entry("mawla", "Membros", "Mawla", false, 1, 5, "1 a 5", "O personagem possui um mentor não-vivo que o auxilia."),
        Entry("status", "Membros", "Status", false, 1, 5, "1 a 5", "Reflete a sua reputação dentro da Seita."),
        Entry("adversary", "Membros", "Adversário", true, 1, 5, "1 a 5", "O personagem possui um inimigo não-vivo atuando contra ele."),
        Entry("suspect", "Membros", "Suspeito", true, 1, 1, "1", "O personagem tem má reputação na seita, sofrendo uma penalidade de -2 dados em testes Sociais com a facção ofendida."),
        Entry("shunned", "Membros", "Segregado", true, 2, 2, "2", "O personagem é totalmente desprezado por uma determinada seita."),
        Entry("living-in-the-past", "Acompanhando o Tempo", "Vivendo no Passado", true, 1, 1, "1", "O personagem possui visões e opiniões ultrapassadas."),
        Entry("archaic", "Acompanhando o Tempo", "Arcaico", true, 2, 2, "2", "A habilidade tecnológica do personagem é fixada em zero."),
        Entry("eat-food", "Mítico", "Ingerir Comida", false, 2, 2, "2", "Permite que o vampiro possa consumir comida humana normal."),
        Entry("folkloric-bane", "Mítico", "Perdição Folclórica", true, 1, 1, "1", "O vampiro toma dano real quando toca um item específico de lendas (como prata, cruz sagrada ou alho)."),
        Entry("folkloric-block", "Mítico", "Tabu Folclórico", true, 1, 1, "1", "Você deve gastar Força de Vontade para atravessar coisas específicas das lendas, como entrar numa casa sem convite ou cruzar água corrente."),
        Entry("stigmata", "Mítico", "Estigma", true, 1, 1, "1", "O vampiro sangra visivelmente pelas mãos, pés e testa sempre que atinge o nível 4 de Fome."),
        Entry("stake-bait", "Mítico", "Isca de Estaca", true, 2, 2, "2", "O personagem encontra a Morte Final (morre de vez) caso seja estacado no coração, em vez de apenas entrar em torpor."),
        Entry("mask", "Máscara", "Máscara", false, 1, 2, "1 a 2", "Fornece uma identidade falsa confiável no mundo mortal."),
        Entry("known-corpse", "Máscara", "Cadáver Identificado", true, 1, 1, "1", "O personagem é sabidamente e publicamente dado como morto."),
        Entry("known-blankbody", "Máscara", "Corpo Frio Identificado", true, 2, 2, "2", "O personagem está registrado em Órgãos Governamentais como sendo uma criatura vampírica."),
        Entry("linguistics", "Linguística", "Linguística", false, 1, 1, "1", "Cada ponto comprado representa um novo idioma no qual você é fluente, além do seu nativo."),
        Entry("illiterate", "Linguística", "Analfabeto", true, 1, 1, "1", "O personagem não sabe ler ou escrever em nenhum idioma."),
        Entry("retainers", "Mortais", "Lacaios", false, 1, 3, "1 a 3", "Concede leais servos mortais ao personagem."),
        Entry("allies", "Mortais", "Aliados", false, 1, 5, "1 a 5", "Representa um grupo de suporte mortal; os pontos indicam a Efetividade ou Confiabilidade deles."),
        Entry("contacts", "Mortais", "Contatos", false, 1, 3, "1 a 3", "Mortais que não lutam, mas fornecem informações, itens ou coisas de valor para você."),
        Entry("herd", "Mortais", "Rebanho", false, 1, 5, "1 a 5", "Um grupo de mortais de onde você pode se alimentar sem preocupações, mas eles são menos leais que Lacaios."),
        Entry("stalkers", "Mortais", "Perseguidores", true, 1, 1, "1", "O personagem tem seguidores mortais indesejados."),
        Entry("enemy", "Mortais", "Inimigo", true, 1, 5, "1+", "O exato oposto dos Aliados; inimigos mortais com grande Efetividade contra você."),
        Entry("obvious-predator", "Mortais", "Predador Manifesto", true, 2, 2, "2", "O vampiro assusta e repele os mortais ao seu redor, sendo incapaz de manter um Rebanho.")
    ];

    private static AdvantageDefinition Entry(string id, string group, string name, bool flaw, int minimum, int maximum,
        string points, string description) => new()
        {
            Id = (flaw ? "flaw-" : "advantage-") + id, Group = group, Name = name, Category = flaw ? TraitCategory.Flaw : TraitCategory.Advantage,
            Minimum = minimum, Maximum = maximum, PointsLabel = points, Description = description
        };
}
