using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VCS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LoresheetsAndCampaignXp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Loresheet",
                table: "CharacterTraits",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Details_CampaignExperienceSpent",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Details_CampaignExperienceTotal",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsLineage",
                table: "AdvantageCatalog",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LoresheetCollection",
                table: "AdvantageCatalog",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-adversario",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-aliados",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-analfabeto",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-arcaico",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-arsenal-oculto",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-assombrado",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-cadaver-identificado",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-comprometido",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-contatos",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-corpo-frio-identificado",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-deslumbrante",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-destituido",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-esofago-de-ferro",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-estigma",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-exclusao-de-presa",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-fazendeiro",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-feio",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-ingerir-comida",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-inimigo",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-isca-de-estaca",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-lacaios",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-linguistica",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-luxo",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-mascara",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-mawla",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-nenhum-refugio",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-organivoro",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-perdicao-folclorica",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-perseguidores",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-predador-manifesto",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-rebanho",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-recursos",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-refugio",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-repulsivo",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-sabujo-de-sangue",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-sede-do-matusalem",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-segregado",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-status",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-suspeito",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-tabu-folclorico",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-vigilancia",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-visual-belo",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.UpdateData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "catalog-vivendo-no-passado",
                columns: new[] { "IsLineage", "LoresheetCollection" },
                values: new object[] { false, "" });

            migrationBuilder.InsertData(
                table: "AdvantageCatalog",
                columns: new[] { "Id", "Category", "Description", "Group", "IsLineage", "LoresheetCollection", "Maximum", "Minimum", "Name", "PointsLabel" },
                values: new object[,]
                {
                    { "lore-bahari-1", 6, "Uma vez por história, concede dois dados extras em testes de Intimidação contra adoradores de Caim que conheçam sua afiliação.", "Ficha de Conhecimento", false, "Os Bahari", 1, 1, "Reputação Perigosa", "1" },
                    { "lore-bahari-2", 6, "Uma vez por sessão, permite que você cause dano Agravado em si mesmo (escarificação) para recuperar um nível de dano (Superficial ou Agravado) na Força de Vontade.", "Ficha de Conhecimento", false, "Os Bahari", 2, 2, "Escarificação Ritual", "2" },
                    { "lore-bahari-3", 6, "Cometer Diablerie em sua própria Criança concede três dados extras na parada de Humanidade + Potência de Sangue para absorver Disciplinas.", "Ficha de Conhecimento", false, "Os Bahari", 3, 3, "Sacrifício dos Filhos", "3" },
                    { "lore-bahari-4", 6, "Uma vez por história, beber sangue do útero concede dois pontos temporários em Vigor ou Determinação (aumentando Vitalidade/Força de Vontade) até o amanhecer.", "Ficha de Conhecimento", false, "Os Bahari", 4, 4, "Sangue do Útero", "4" },
                    { "lore-bahari-5", 6, "Permite andar na primeira/última hora de sol e fazer sexo sem Inflamar o Sangue, mas você atrai o defeito Predador Manifesto, falha mais fácil em testes de difamação e causa enxaqueca em quem usar Auspícios em você.", "Ficha de Conhecimento", false, "Os Bahari", 5, 5, "Amaldiçoada Original", "5" },
                    { "lore-carna-1", 6, "O conforto entre colegas de subfacção lhe garante um dado adicional em todos os testes de Força de Vontade perto deles.", "Ficha de Conhecimento", false, "Carna", 1, 1, "Abraçar a Visão", "1" },
                    { "lore-carna-2", 6, "Permite rolar Força de Vontade (Dificuldade igual à Potência do Sangue ingerido) para ignorar completamente o risco de criar um Laço de Sangue.", "Ficha de Conhecimento", false, "Carna", 2, 2, "O Rastro Rebelde", "2" },
                    { "lore-carna-3", 6, "Uma vez por história, realiza um ritual sem Checagem de Sangue, mas um Crítico Bestial causa um trauma/paranoia temporária até o fim da história.", "Ficha de Conhecimento", false, "Carna", 3, 3, "Rituais Heterodoxos", "3" },
                    { "lore-carna-4", 6, "Transar com um alvo contorna a perdição Tremere e cria um Laço de Sangue temporário (até o fim da história) entre você, o alvo e Carna.", "Ficha de Conhecimento", false, "Carna", 4, 4, "Laço Reimaginado", "4" },
                    { "lore-carna-5", 6, "Imuniza contra Laços de Sangue e dá sucesso automático em testes de Ocultismo sobre a Gehenna, mas a paranoia custa 1 dado em testes Sociais/Mentais.", "Ficha de Conhecimento", false, "Carna", 5, 5, "Livro da Guerra dos Túmulos", "5" },
                    { "lore-heresia-cainita-1", 6, "Uma vez por história, o Narrador é obrigado a fornecer uma pista grátis para detectar ou entender os planos da Heresia.", "Ficha de Conhecimento", false, "Heresia Cainita", 1, 1, "Que Ele tenha Compreensão", "1" },
                    { "lore-heresia-cainita-2", 6, "Garante três pontos para distribuir entre Aliados, Rebanho, Mawla ou Lacaios no culto, mas aplica o defeito Segredo Sombrio (Heresia).", "Ficha de Conhecimento", false, "Heresia Cainita", 2, 2, "Mão da Heresia", "2" },
                    { "lore-heresia-cainita-3", 6, "Permite usar Determinação + Heresia Cainita para detectar passivamente a Fé Verdadeira em mortais ou saber em quem confiar.", "Ficha de Conhecimento", false, "Heresia Cainita", 3, 3, "Contra-Inquisição", "3" },
                    { "lore-heresia-cainita-4", 6, "Uma vez por história, induz humanos a um frenesi extático (Pentecostes Vermelho) com ervas, custando uma Checagem de Sangue e podendo causar frenesi em Membros.", "Ficha de Conhecimento", false, "Heresia Cainita", 4, 4, "Celebrante Vermelho", "4" },
                    { "lore-heresia-cainita-5", 6, "Uma vez por história, devido à sua importância nas profecias, você pode ditar o vencedor de um conflito Social fornecendo uma desculpa plausível.", "Ficha de Conhecimento", false, "Heresia Cainita", 5, 5, "Nomeado na Profecia", "5" },
                    { "lore-theo-bell-1", 6, "Fornece um grupo mortal rebelde (equivalente a Aliado de 3 pontos) que fará uma missão perigosa sozinha para você antes de debandar até a próxima história.", "Ficha de Conhecimento", false, "Theo Bell", 1, 1, "Célula Rebelde", "1" },
                    { "lore-theo-bell-2", 6, "Garante dois sucessos automáticos em qualquer teste investigativo envolvendo vampiros que desertaram da Camarilla para o Movimento Anarch.", "Ficha de Conhecimento", false, "Theo Bell", 2, 2, "Anarch Verdadeiro", "2" },
                    { "lore-theo-bell-3", 6, "Permite contatar Theo Bell para enviar uma mensagem; os efeitos e a resposta dependem do Narrador e da importância do recado.", "Ficha de Conhecimento", false, "Theo Bell", 3, 3, "Informação de Contato", "3" },
                    { "lore-theo-bell-4", 6, "Transforma Theo Bell em um Mawla de 5 pontos, trazendo conselhos valiosos, mas também complicações e perigos devido à associação com ele.", "Ficha de Conhecimento", false, "Theo Bell", 4, 4, "Círculo de Bell", "4" },
                    { "lore-theo-bell-5", 6, "Concede seguidores Brujah leais e cinco pontos para distribuir livremente entre Contatos, Refúgio, Mawla e Lacaios até que eles decidam se rebelar.", "Ficha de Conhecimento", false, "Theo Bell", 5, 5, "Neutralidade de Seita", "5" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-bahari-1");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-bahari-2");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-bahari-3");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-bahari-4");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-bahari-5");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-carna-1");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-carna-2");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-carna-3");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-carna-4");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-carna-5");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-heresia-cainita-1");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-heresia-cainita-2");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-heresia-cainita-3");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-heresia-cainita-4");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-heresia-cainita-5");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-theo-bell-1");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-theo-bell-2");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-theo-bell-3");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-theo-bell-4");

            migrationBuilder.DeleteData(
                table: "AdvantageCatalog",
                keyColumn: "Id",
                keyValue: "lore-theo-bell-5");

            migrationBuilder.DropColumn(
                name: "Loresheet",
                table: "CharacterTraits");

            migrationBuilder.DropColumn(
                name: "Details_CampaignExperienceSpent",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_CampaignExperienceTotal",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "IsLineage",
                table: "AdvantageCatalog");

            migrationBuilder.DropColumn(
                name: "LoresheetCollection",
                table: "AdvantageCatalog");
        }
    }
}
