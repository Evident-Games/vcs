using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VCS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SkillsAndNpcSheets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Details_IsNpc",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Details_NpcHealthCapacity",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "Details_NpcMental",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Details_NpcPhysical",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Details_NpcSocial",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Details_NpcWillpowerCapacity",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.CreateTable(
                name: "SkillCatalog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Group = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillCatalog", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SkillCatalog",
                columns: new[] { "Id", "Description", "Group", "Name" },
                values: new object[,]
                {
                    { "skill-academics", "Representa uma instrução superior adquirida via dedicação acadêmica ou em mentorias ao longo das eras. Rege as áreas de humanas, focando em interpretação de história de arte, jornalismo, literatura e teologia profunda.", "Mentais", "Erudição" },
                    { "skill-animal-ken", "Concede a aptidão para pacificar, se aproximar, treinar e prever os humores dos animais de forma que eles não fujam ou ataquem instintivamente o personagem devido à sua condição morta-viva.", "Sociais", "Empatia com Animais" },
                    { "skill-athletics", "Define a capacidade de correr, saltar, nadar e fazer acrobacias ou manobras de parkour. Também pode ser usada de forma defensiva como esquiva em substituição às habilidades de ataque durante combates.", "Físicas", "Atletismo" },
                    { "skill-awareness", "O uso atento, focado ou automático de instintos corporais para vigiar os arredores. Detecta desde pistas visuais em cantos escuros até aproximações sorrateiras, odores estranhos persistentes ou emboscadas invisíveis.", "Mentais", "Percepção" },
                    { "skill-brawl", "A arte pura do combate desarmado para lutar e nocautear. Abrange desde as táticas sujas de brigas de rua até estilos formais e marciais usando punhos, chutes ou golpes de garras.", "Físicas", "Briga" },
                    { "skill-craft", "Representa a criatividade e a execução de trabalhos artesanais do personagem, seja esculpindo, costurando e forjando armas, ou até mesmo estruturando armadilhas e erguendo o próprio refúgio.", "Físicas", "Ofícios" },
                    { "skill-drive", "A capacidade de pilotar e controlar os mais diversos veículos com destreza e segurança em velocidades extremas. É uma habilidade vital para fugas, perseguições urbanas e manobras sob intenso estresse.", "Físicas", "Condução" },
                    { "skill-etiquette", "Compreensão das normas, regras de deferência e dos costumes adequados a cada contexto da sociedade. Evita gafes mortais nas cortes dos Príncipes ou entre as elites de alto poder aquisitivo no mundo humano.", "Sociais", "Etiqueta" },
                    { "skill-finance", "Habilidade imperativa aos Ventrue modernos voltada a avaliar obras de arte, administrar grandes portfólios corporativos, manipular transações ilegais invisíveis e criar empresas para lavar montanhas de dinheiro.", "Mentais", "Finanças" },
                    { "skill-firearms", "Engloba a manutenção (limpeza e destravamento), a recarga ágil e o disparo de precisão de armamentos que usam gatilho, passando por pistolas, fuzis e lançadores de granada ou bestas.", "Físicas", "Armas de Fogo" },
                    { "skill-insight", "Atua como um dom investigativo empático na percepção humana, decifrando intenções escondidas, fobias ocultas, mentiras escancaradas ou nuances da linguagem não-verbal nos inimigos.", "Sociais", "Sagacidade" },
                    { "skill-intimidation", "O instinto de subjugar outras mentes de forma brutal através do poder da opressão, olhares fixos, xingamentos ríspidos ou da promessa de coerção e extrema violência física.", "Sociais", "Intimidação" },
                    { "skill-investigation", "A proficiência forense focada em notar coisas fora do padrão mundano. É vital para caçar indivíduos desaparecidos, ler cenas de assassinatos com olhar criminal e interrogar pistas sem levantar suspeitas do alvo real.", "Mentais", "Investigação" },
                    { "skill-larceny", "Intimidade prática com invasões. Permite bater carteiras na rua, abrir cofres antigos ou eletrônicos, forçar cadeados e fechaduras, fazer ligações diretas em carros e burlar alarmes de invasão domiciliar.", "Físicas", "Ladroagem" },
                    { "skill-leadership", "A capacidade política de guiar grupos de mortais e vampiros de forma cativante. É voltada a organizar manifestações, elevar moralmente os aliados e liderar bandos em campos de guerra violentos.", "Sociais", "Liderança" },
                    { "skill-medicine", "Conhecimento vital ou traumatológico usado clinicamente para diagnosticar mortes e prever anomalias de sangue e veias. Na prática, é aplicada em cirurgias severas e curativos de urgência que estabilizam danos agravados na saúde de mortais.", "Mentais", "Medicina" },
                    { "skill-melee", "Permite o manejo ágil e letal de armas portáteis focadas no combate corpo a corpo, englobando desde facas e espadas até correntes, machados e estacas de caçadores.", "Físicas", "Armas Brancas" },
                    { "skill-occult", "A sabedoria essencial sobre todos os aspectos do Mundo das Trevas, englobando táticas contra fantasmas, lobisomens, parapsicologia esotérica e até conhecimentos obscuros e proibidos como Magia de Sangue e textos nodistas.", "Mentais", "Ocultismo" },
                    { "skill-performance", "A expressão magnética e o virtuosismo das belas artes, englobando a apresentação cênica, dança, oratória, poesia e comédia para entreter, emocionar ou impressionar espectadores.", "Sociais", "Performance" },
                    { "skill-persuasion", "O dom prático de argumentar com eloquência em negociações e barganhas, convencendo o outro ouvidor de que a intenção do vampiro — de uma assinatura de acordo a um gole de sangue — não representa perigo e faz sentido.", "Sociais", "Persuasão" },
                    { "skill-politics", "Compreensão técnica da diplomacia e da mecânica dos governos de gabinete e sindicatos na prefeitura. Envolve rastrear esquemas de poder da Mídia, dominar cargos burocráticos e jogar a guerra dos tronos nas diferentes Seitas cainitas e mortais.", "Mentais", "Política" },
                    { "skill-science", "Estudo focado das leis lógicas e das engrenagens universais. Envolve conhecimento laboratorial prático ou acadêmico em química, genoma genético molecular, matemática, física e engenharia pesada.", "Mentais", "Ciência" },
                    { "skill-stealth", "O talento imperceptível do caçador para se esgueirar pelas sombras, evitar rondas de segurança, esconder-se do olhar humano e rastrear ou perseguir presas de forma sorrateira.", "Físicas", "Furtividade" },
                    { "skill-streetwise", "O domínio nativo sobre as complexas regras e códigos do submundo local. Facilita a comunicação nas ruas por pichações de gangue, indica onde adquirir suprimentos vitais no mercado negro ou recrutar criminosos.", "Sociais", "Manha" },
                    { "skill-subterfuge", "O talento indispensável para forjar redes de enganos duradouras e proteger o disfarce pessoal, seduzindo, contando falsidades de maneira fria e escondendo crimes atrás de uma máscara de completa inocência.", "Sociais", "Subterfúgio" },
                    { "skill-survival", "Define a adaptabilidade em áreas selvagens rurais ou nas entranhas selva urbana. Facilita caçar fora das metrópoles, montar abrigos naturais de emergência e rastrear perigos como lobisomens.", "Físicas", "Sobrevivência" },
                    { "skill-technology", "A capacidade pragmática ou engenhosa no mundo contemporâneo para lidar com inovações. Envolve operações furtivas como hackear servidores corporativos em rede local, sumir na vigilância cibernética de metrópoles, montar computadores blindados e decodificar dados restritos.", "Mentais", "Tecnologia" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkillCatalog");

            migrationBuilder.DropColumn(
                name: "Details_IsNpc",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_NpcHealthCapacity",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_NpcMental",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_NpcPhysical",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_NpcSocial",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_NpcWillpowerCapacity",
                table: "Characters");
        }
    }
}
