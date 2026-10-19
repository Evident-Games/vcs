using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VCS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdvantageCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdvantageCatalog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Group = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Minimum = table.Column<int>(type: "INTEGER", nullable: false),
                    Maximum = table.Column<int>(type: "INTEGER", nullable: false),
                    PointsLabel = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvantageCatalog", x => x.Id);
                    table.CheckConstraint("CK_AdvantageCatalog_Points", "\"Minimum\" >= 1 AND \"Maximum\" >= \"Minimum\" AND \"Maximum\" <= 5");
                });

            migrationBuilder.InsertData(
                table: "AdvantageCatalog",
                columns: new[] { "Id", "Category", "Description", "Group", "Maximum", "Minimum", "Name", "PointsLabel" },
                values: new object[,]
                {
                    { "catalog-adversario", 4, "O personagem possui um inimigo não-vivo atuando contra ele.", "Membros", 5, 1, "Adversário", "1 a 5" },
                    { "catalog-aliados", 3, "Representa um grupo de suporte mortal; os pontos indicam a Efetividade ou Confiabilidade deles.", "Mortais", 5, 1, "Aliados", "1 a 5" },
                    { "catalog-analfabeto", 4, "O personagem não sabe ler ou escrever em nenhum idioma.", "Linguística", 1, 1, "Analfabeto", "1" },
                    { "catalog-arcaico", 4, "A habilidade tecnológica do personagem é fixada em zero.", "Acompanhando o Tempo", 2, 2, "Arcaico", "2" },
                    { "catalog-arsenal-oculto", 3, "Adiciona armas de fogo extras ao seu refúgio.", "Refúgio", 1, 1, "Arsenal oculto", "1" },
                    { "catalog-assombrado", 4, "O refúgio possui uma presença fantasmagórica.", "Refúgio", 1, 1, "Assombrado", "1" },
                    { "catalog-cadaver-identificado", 4, "O personagem é sabidamente e publicamente dado como morto.", "Máscara", 1, 1, "Cadáver Identificado", "1" },
                    { "catalog-comprometido", 4, "O refúgio está sob vigilância de outras pessoas.", "Refúgio", 2, 2, "Comprometido", "2" },
                    { "catalog-contatos", 3, "Mortais que não lutam, mas fornecem informações, itens ou coisas de valor para você.", "Mortais", 3, 1, "Contatos", "1 a 3" },
                    { "catalog-corpo-frio-identificado", 4, "O personagem está registrado em Órgãos Governamentais como sendo uma criatura vampírica.", "Máscara", 2, 2, "Corpo Frio Identificado", "2" },
                    { "catalog-deslumbrante", 3, "Adiciona +2 dados em todos os testes Sociais do personagem.", "Visual", 4, 4, "Deslumbrante", "4" },
                    { "catalog-destituido", 4, "O personagem não tem dinheiro e nem emprego.", "Recursos", 1, 1, "Destituído", "1" },
                    { "catalog-esofago-de-ferro", 3, "O vampiro é capaz de consumir sangue rançoso ou fracionado sem problemas.", "Alimentação", 3, 3, "Esôfago de Ferro", "3" },
                    { "catalog-estigma", 4, "O vampiro sangra visivelmente pelas mãos, pés e testa sempre que atinge o nível 4 de Fome.", "Mítico", 1, 1, "Estigma", "1" },
                    { "catalog-exclusao-de-presa", 4, "Torna o personagem incapaz de se alimentar de um determinado grupo específico de pessoas.", "Alimentação", 1, 1, "Exclusão de presa", "1" },
                    { "catalog-fazendeiro", 4, "Exige que você gaste 2 pontos de Força de Vontade toda vez que quiser beber sangue que não seja de animais.", "Alimentação", 2, 2, "Fazendeiro", "2" },
                    { "catalog-feio", 4, "Subtrai 1 dado em todos os testes Sociais.", "Visual", 1, 1, "Feio", "1" },
                    { "catalog-ingerir-comida", 3, "Permite que o vampiro possa consumir comida humana normal.", "Mítico", 2, 2, "Ingerir Comida", "2" },
                    { "catalog-inimigo", 4, "O exato oposto dos Aliados; inimigos mortais com grande Efetividade contra você.", "Mortais", 5, 1, "Inimigo", "1+" },
                    { "catalog-isca-de-estaca", 4, "O personagem encontra a Morte Final (morre de vez) caso seja estacado no coração, em vez de apenas entrar em torpor.", "Mítico", 2, 2, "Isca de Estaca", "2" },
                    { "catalog-lacaios", 3, "Concede leais servos mortais ao personagem.", "Mortais", 3, 1, "Lacaios", "1 a 3" },
                    { "catalog-linguistica", 3, "Cada ponto comprado representa um novo idioma no qual você é fluente, além do seu nativo.", "Linguística", 1, 1, "Linguística", "1" },
                    { "catalog-luxo", 3, "Garante +2 dados em testes sociais que sejam realizados no interior do refúgio.", "Refúgio", 1, 1, "Luxo", "1" },
                    { "catalog-mascara", 3, "Fornece uma identidade falsa confiável no mundo mortal.", "Máscara", 2, 1, "Máscara", "1 a 2" },
                    { "catalog-mawla", 3, "O personagem possui um mentor não-vivo que o auxilia.", "Membros", 5, 1, "Mawla", "1 a 5" },
                    { "catalog-nenhum-refugio", 4, "O personagem não possui um local seguro fixo.", "Refúgio", 1, 1, "Nenhum Refúgio", "1" },
                    { "catalog-organivoro", 4, "O vampiro só consegue aplacar a Fome consumindo carne ou órgãos humanos.", "Alimentação", 2, 2, "Organívoro", "2" },
                    { "catalog-perdicao-folclorica", 4, "O vampiro toma dano real quando toca um item específico de lendas (como prata, cruz sagrada ou alho).", "Mítico", 1, 1, "Perdição Folclórica", "1" },
                    { "catalog-perseguidores", 4, "O personagem tem seguidores mortais indesejados.", "Mortais", 1, 1, "Perseguidores", "1" },
                    { "catalog-predador-manifesto", 4, "O vampiro assusta e repele os mortais ao seu redor, sendo incapaz de manter um Rebanho.", "Mortais", 2, 2, "Predador Manifesto", "2" },
                    { "catalog-rebanho", 3, "Um grupo de mortais de onde você pode se alimentar sem preocupações, mas eles são menos leais que Lacaios.", "Mortais", 5, 1, "Rebanho", "1 a 5" },
                    { "catalog-recursos", 3, "Representa o seu fluxo de caixa e dinheiro disponível.", "Recursos", 5, 1, "Recursos", "1 a 5" },
                    { "catalog-refugio", 3, "Fornece ao seu personagem um lugar seguro.", "Refúgio", 3, 1, "Refúgio", "1 a 3" },
                    { "catalog-repulsivo", 4, "Subtrai 2 dados em todos os testes Sociais.", "Visual", 2, 2, "Repulsivo", "2" },
                    { "catalog-sabujo-de-sangue", 3, "O personagem é capaz de farejar a Ressonância do sangue humano.", "Alimentação", 1, 1, "Sabujo de Sangue", "1" },
                    { "catalog-sede-do-matusalem", 4, "O personagem é incapaz de se saciar completamente apenas bebendo sangue humano.", "Alimentação", 1, 1, "Sede do Matusalém", "1" },
                    { "catalog-segregado", 4, "O personagem é totalmente desprezado por uma determinada seita.", "Membros", 2, 2, "Segregado", "2" },
                    { "catalog-status", 3, "Reflete a sua reputação dentro da Seita.", "Membros", 5, 1, "Status", "1 a 5" },
                    { "catalog-suspeito", 4, "O personagem tem má reputação na seita, sofrendo uma penalidade de -2 dados em testes Sociais com a facção ofendida.", "Membros", 1, 1, "Suspeito", "1" },
                    { "catalog-tabu-folclorico", 4, "Você deve gastar Força de Vontade para atravessar coisas específicas das lendas, como entrar numa casa sem convite ou cruzar água corrente.", "Mítico", 1, 1, "Tabu Folclórico", "1" },
                    { "catalog-vigilancia", 3, "Fornece seguranças humanos para o refúgio.", "Refúgio", 1, 1, "Vigilância", "1" },
                    { "catalog-visual-belo", 3, "Adiciona +1 dado em todos os testes Sociais do personagem.", "Visual", 2, 2, "Visual Belo", "2" },
                    { "catalog-vivendo-no-passado", 4, "O personagem possui visões e opiniões ultrapassadas.", "Acompanhando o Tempo", 1, 1, "Vivendo no Passado", "1" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdvantageCatalog");
        }
    }
}
