using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VCS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContentIdsAdvantagesAndPredators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Built-in advantages, flaws and loresheets keep their rows (and any user edits): only the id changes.
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-looks-beautiful' WHERE \"Id\" = 'catalog-visual-belo';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-looks-stunning' WHERE \"Id\" = 'catalog-deslumbrante';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-looks-ugly' WHERE \"Id\" = 'catalog-feio';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-looks-repulsive' WHERE \"Id\" = 'catalog-repulsivo';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-haven' WHERE \"Id\" = 'catalog-refugio';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-haven-hidden-armory' WHERE \"Id\" = 'catalog-arsenal-oculto';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-haven-watchmen' WHERE \"Id\" = 'catalog-vigilancia';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-haven-luxury' WHERE \"Id\" = 'catalog-luxo';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-no-haven' WHERE \"Id\" = 'catalog-nenhum-refugio';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-haven-haunted' WHERE \"Id\" = 'catalog-assombrado';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-haven-compromised' WHERE \"Id\" = 'catalog-comprometido';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-resources' WHERE \"Id\" = 'catalog-recursos';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-destitute' WHERE \"Id\" = 'catalog-destituido';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-bloodhound' WHERE \"Id\" = 'catalog-sabujo-de-sangue';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-iron-gullet' WHERE \"Id\" = 'catalog-esofago-de-ferro';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-prey-exclusion' WHERE \"Id\" = 'catalog-exclusao-de-presa';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-methuselahs-thirst' WHERE \"Id\" = 'catalog-sede-do-matusalem';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-farmer' WHERE \"Id\" = 'catalog-fazendeiro';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-organovore' WHERE \"Id\" = 'catalog-organivoro';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-mawla' WHERE \"Id\" = 'catalog-mawla';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-status' WHERE \"Id\" = 'catalog-status';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-adversary' WHERE \"Id\" = 'catalog-adversario';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-suspect' WHERE \"Id\" = 'catalog-suspeito';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-shunned' WHERE \"Id\" = 'catalog-segregado';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-living-in-the-past' WHERE \"Id\" = 'catalog-vivendo-no-passado';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-archaic' WHERE \"Id\" = 'catalog-arcaico';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-eat-food' WHERE \"Id\" = 'catalog-ingerir-comida';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-folkloric-bane' WHERE \"Id\" = 'catalog-perdicao-folclorica';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-folkloric-block' WHERE \"Id\" = 'catalog-tabu-folclorico';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-stigmata' WHERE \"Id\" = 'catalog-estigma';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-stake-bait' WHERE \"Id\" = 'catalog-isca-de-estaca';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-mask' WHERE \"Id\" = 'catalog-mascara';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-known-corpse' WHERE \"Id\" = 'catalog-cadaver-identificado';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-known-blankbody' WHERE \"Id\" = 'catalog-corpo-frio-identificado';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-linguistics' WHERE \"Id\" = 'catalog-linguistica';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-illiterate' WHERE \"Id\" = 'catalog-analfabeto';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-retainers' WHERE \"Id\" = 'catalog-lacaios';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-allies' WHERE \"Id\" = 'catalog-aliados';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-contacts' WHERE \"Id\" = 'catalog-contatos';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'advantage-herd' WHERE \"Id\" = 'catalog-rebanho';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-stalkers' WHERE \"Id\" = 'catalog-perseguidores';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-enemy' WHERE \"Id\" = 'catalog-inimigo';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'flaw-obvious-predator' WHERE \"Id\" = 'catalog-predador-manifesto';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-bahari-1' WHERE \"Id\" = 'lore-bahari-1';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-bahari-2' WHERE \"Id\" = 'lore-bahari-2';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-bahari-3' WHERE \"Id\" = 'lore-bahari-3';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-bahari-4' WHERE \"Id\" = 'lore-bahari-4';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-bahari-5' WHERE \"Id\" = 'lore-bahari-5';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-theo-bell-1' WHERE \"Id\" = 'lore-theo-bell-1';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-theo-bell-2' WHERE \"Id\" = 'lore-theo-bell-2';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-theo-bell-3' WHERE \"Id\" = 'lore-theo-bell-3';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-theo-bell-4' WHERE \"Id\" = 'lore-theo-bell-4';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-theo-bell-5' WHERE \"Id\" = 'lore-theo-bell-5';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-cainite-heresy-1' WHERE \"Id\" = 'lore-heresia-cainita-1';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-cainite-heresy-2' WHERE \"Id\" = 'lore-heresia-cainita-2';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-cainite-heresy-3' WHERE \"Id\" = 'lore-heresia-cainita-3';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-cainite-heresy-4' WHERE \"Id\" = 'lore-heresia-cainita-4';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-cainite-heresy-5' WHERE \"Id\" = 'lore-heresia-cainita-5';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-carna-1' WHERE \"Id\" = 'lore-carna-1';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-carna-2' WHERE \"Id\" = 'lore-carna-2';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-carna-3' WHERE \"Id\" = 'lore-carna-3';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-carna-4' WHERE \"Id\" = 'lore-carna-4';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'loresheet-carna-5' WHERE \"Id\" = 'lore-carna-5';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = CASE \"Category\" WHEN 4 THEN 'flaw-custom-' WHEN 6 THEN 'loresheet-custom-' ELSE 'advantage-custom-' END || substr(\"Id\", 12) WHERE \"Id\" LIKE 'adv-custom-%';");

            // The predator catalog is read-only seed data: replace it.
            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "alleycat");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "bagger");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "blood-leech");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "cleaver");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "consensualist");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "extortionist");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "farmer");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "graverobber");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "osiris");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "roadside");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "sandman");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "scene-queen");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "siren");

            migrationBuilder.InsertData(
                table: "PredatorCatalog",
                columns: new[] { "Id", "Definition", "Name" },
                values: new object[,]
                {
                    { "predator-alleycat", "{\"Id\":\"predator-alleycat\",\"Name\":\"Vira-lata\",\"Description\":\"Um ca\\u00E7ador agressivo que ataca suas v\\u00EDtimas nas ruas, seja disfar\\u00E7ando a ca\\u00E7ada como um assalto ou dominando sua presa pela for\\u00E7a.\",\"Benefits\":\"\\u002B1 Celeridade ou Pot\\u00EAncia; \\u002B3 Contatos Criminosos; especializa\\u00E7\\u00E3o em Intimida\\u00E7\\u00E3o (Assalto \\u00E0 M\\u00E3o Armada) ou Briga (Agarramento).\",\"Cost\":\"Perde 1 ponto de Humanidade.\",\"Disciplines\":[\"Celeridade\",\"Pot\\u00EAncia\"],\"Specializations\":[{\"Skill\":\"Intimida\\u00E7\\u00E3o\",\"Text\":\"Assalto \\u00E0 M\\u00E3o Armada\",\"IsFreeText\":false,\"Label\":\"Intimida\\u00E7\\u00E3o (Assalto \\u00E0 M\\u00E3o Armada)\"},{\"Skill\":\"Briga\",\"Text\":\"Agarramento\",\"IsFreeText\":false,\"Label\":\"Briga (Agarramento)\"}],\"FixedGrants\":[{\"Name\":\"Contatos Criminosos\",\"Category\":3}],\"FixedPoints\":[3],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":-1,\"BloodDelta\":0}", "Vira-lata" },
                    { "predator-bagger", "{\"Id\":\"predator-bagger\",\"Name\":\"Sacoleiro\",\"Description\":\"Evita viol\\u00EAncia de ca\\u00E7a real, adquirindo sangue empacotado em hospitais ou contrabandeando do mercado negro. Ventrue n\\u00E3o pode escolher.\",\"Benefits\":\"\\u002B1 Ofusca\\u00E7\\u00E3o, Obl\\u00EDvio ou Feiti\\u00E7aria de Sangue (s\\u00F3 Tremere); Es\\u00F4fago de Ferro 3; especializa\\u00E7\\u00E3o em Ladroagem (Abrir Fechaduras) ou Manha (Mercado Negro).\",\"Cost\":\"Inimigo 2 (cobrando d\\u00EDvidas ou banindo-o das ruas).\",\"Disciplines\":[\"Ofusca\\u00E7\\u00E3o\",\"Obl\\u00EDvio\",\"Feiti\\u00E7aria de Sangue\"],\"Specializations\":[{\"Skill\":\"Ladroagem\",\"Text\":\"Abrir Fechaduras\",\"IsFreeText\":false,\"Label\":\"Ladroagem (Abrir Fechaduras)\"},{\"Skill\":\"Manha\",\"Text\":\"Mercado Negro\",\"IsFreeText\":false,\"Label\":\"Manha (Mercado Negro)\"}],\"FixedGrants\":[{\"Name\":\"Esofago de Ferro\",\"Category\":3},{\"Name\":\"Inimigo (dividas ou banimento)\",\"Category\":4}],\"FixedPoints\":[3,2],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":0,\"BloodDelta\":0}", "Sacoleiro" },
                    { "predator-blood-leech", "{\"Id\":\"predator-blood-leech\",\"Name\":\"Sanguessuga\",\"Description\":\"Um canibal de Membros e Diablerista ocasional; detesta se alimentar de humanos e busca sangue de outros vampiros.\",\"Benefits\":\"\\u002B1 Celeridade ou Proteanismo; \\u002B1 Pot\\u00EAncia de Sangue; especializa\\u00E7\\u00E3o em Briga (Membros) ou Furtividade (Contra Membros).\",\"Cost\":\"-1 Humanidade; Segredo Sombrio 2 (Diablerista) OU Segregado 2; Exclus\\u00E3o de Presa 2 (Mortais).\",\"Disciplines\":[\"Celeridade\",\"Proteanismo\"],\"Specializations\":[{\"Skill\":\"Briga\",\"Text\":\"Membros\",\"IsFreeText\":false,\"Label\":\"Briga (Membros)\"},{\"Skill\":\"Furtividade\",\"Text\":\"Contra Membros\",\"IsFreeText\":false,\"Label\":\"Furtividade (Contra Membros)\"}],\"FixedGrants\":[{\"Name\":\"Exclusao de Presa (Mortais)\",\"Category\":4}],\"FixedPoints\":[2],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[{\"Name\":\"Segredo Sombrio (Diablerista)\",\"Category\":4},{\"Name\":\"Segregado\",\"Category\":4}],\"FlawPoints\":2,\"SplitFlaws\":false,\"HumanityDelta\":-1,\"BloodDelta\":1}", "Sanguessuga" },
                    { "predator-cleaver", "{\"Id\":\"predator-cleaver\",\"Name\":\"Trinchador\",\"Description\":\"Um parasita familiar; esconde a sua natureza bestial de seus amigos e familiares mundanos, enquanto se alimenta secretamente deles no conv\\u00EDvio di\\u00E1rio.\",\"Benefits\":\"\\u002B1 Domina\\u00E7\\u00E3o ou Animalismo; \\u002B2 Rebanho; especializa\\u00E7\\u00E3o em Persuas\\u00E3o (Gaslighting) ou Subterf\\u00FAgio (Encobrimento).\",\"Cost\":\"Segredo Sombrio 1: Trinchador.\",\"Disciplines\":[\"Domina\\u00E7\\u00E3o\",\"Animalismo\"],\"Specializations\":[{\"Skill\":\"Persuas\\u00E3o\",\"Text\":\"Gaslighting\",\"IsFreeText\":false,\"Label\":\"Persuas\\u00E3o (Gaslighting)\"},{\"Skill\":\"Subterf\\u00FAgio\",\"Text\":\"Encobrimento\",\"IsFreeText\":false,\"Label\":\"Subterf\\u00FAgio (Encobrimento)\"}],\"FixedGrants\":[{\"Name\":\"Rebanho\",\"Category\":3},{\"Name\":\"Segredo Sombrio (Trinchador)\",\"Category\":4}],\"FixedPoints\":[2,1],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":0,\"BloodDelta\":0}", "Trinchador" },
                    { "predator-consensualist", "{\"Id\":\"predator-consensualist\",\"Name\":\"Consensualista\",\"Description\":\"S\\u00F3 se alimenta do sangue de pessoas que aceitam e oferecem livremente; pode dizer a verdade ou se disfar\\u00E7ar de trabalhador de caridade/adepto de cena fetiche vamp\\u00EDrica.\",\"Benefits\":\"\\u002B1 Ausp\\u00EDcios ou Fortitude; \\u002B1 Humanidade; especializa\\u00E7\\u00E3o em Medicina (Flebotomia) ou Persuas\\u00E3o (Bolsas).\",\"Cost\":\"Segredo Sombrio 1: Quebrador da M\\u00E1scara; Exclus\\u00E3o de Presa 1 (sem consentimento).\",\"Disciplines\":[\"Ausp\\u00EDcios\",\"Fortitude\"],\"Specializations\":[{\"Skill\":\"Medicina\",\"Text\":\"Flebotomia\",\"IsFreeText\":false,\"Label\":\"Medicina (Flebotomia)\"},{\"Skill\":\"Persuas\\u00E3o\",\"Text\":\"Bolsas\",\"IsFreeText\":false,\"Label\":\"Persuas\\u00E3o (Bolsas)\"}],\"FixedGrants\":[{\"Name\":\"Segredo Sombrio (Quebrador da Mascara)\",\"Category\":4},{\"Name\":\"Exclusao de Presa (sem consentimento)\",\"Category\":4}],\"FixedPoints\":[1,1],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":1,\"BloodDelta\":0}", "Consensualista" },
                    { "predator-extortionist", "{\"Id\":\"predator-extortionist\",\"Name\":\"Chantagista\",\"Description\":\"Adquire sangue mediante amea\\u00E7as e extors\\u00E3o.\",\"Benefits\":\"\\u002B1 Domina\\u00E7\\u00E3o ou Pot\\u00EAncia; 3 pontos entre Contatos e Recursos; especializa\\u00E7\\u00E3o em Intimida\\u00E7\\u00E3o (Coer\\u00E7\\u00E3o) ou Ladroagem (Seguran\\u00E7a).\",\"Cost\":\"Inimigo 2 (a v\\u00EDtima extorquida ou a pol\\u00EDcia).\",\"Disciplines\":[\"Domina\\u00E7\\u00E3o\",\"Pot\\u00EAncia\"],\"Specializations\":[{\"Skill\":\"Intimida\\u00E7\\u00E3o\",\"Text\":\"Coer\\u00E7\\u00E3o\",\"IsFreeText\":false,\"Label\":\"Intimida\\u00E7\\u00E3o (Coer\\u00E7\\u00E3o)\"},{\"Skill\":\"Ladroagem\",\"Text\":\"Seguran\\u00E7a\",\"IsFreeText\":false,\"Label\":\"Ladroagem (Seguran\\u00E7a)\"}],\"FixedGrants\":[{\"Name\":\"Inimigo (extorsao)\",\"Category\":4}],\"FixedPoints\":[2],\"AllocationOptions\":[{\"Name\":\"Contatos\",\"Category\":3},{\"Name\":\"Recursos\",\"Category\":3}],\"AllocationPoints\":3,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":0,\"BloodDelta\":0}", "Chantagista" },
                    { "predator-farmer", "{\"Id\":\"predator-farmer\",\"Name\":\"Fazendeiro\",\"Description\":\"N\\u00E3o ataca seres humanos e foca sua alimenta\\u00E7\\u00E3o em animais. Ventrue e Membros com Pot\\u00EAncia de Sangue 3\\u002B n\\u00E3o podem escolher.\",\"Benefits\":\"\\u002B1 Animalismo ou Proteanismo; \\u002B1 Humanidade; especializa\\u00E7\\u00E3o em Empatia com Animais (escolha um) ou Sobreviv\\u00EAncia (Ca\\u00E7a).\",\"Cost\":\"Defeito de Alimenta\\u00E7\\u00E3o 2: Fazendeiro.\",\"Disciplines\":[\"Animalismo\",\"Proteanismo\"],\"Specializations\":[{\"Skill\":\"Empatia com Animais\",\"Text\":\"animal a informar\",\"IsFreeText\":true,\"Label\":\"Empatia com Animais (animal a informar)\"},{\"Skill\":\"Sobreviv\\u00EAncia\",\"Text\":\"Ca\\u00E7a\",\"IsFreeText\":false,\"Label\":\"Sobreviv\\u00EAncia (Ca\\u00E7a)\"}],\"FixedGrants\":[{\"Name\":\"Fazendeiro\",\"Category\":4}],\"FixedPoints\":[2],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":1,\"BloodDelta\":0}", "Fazendeiro" },
                    { "predator-graverobber", "{\"Id\":\"predator-graverobber\",\"Name\":\"Ladr\\u00E3o de Sepultura\",\"Description\":\"Restringe-se a ca\\u00E7ar presas nos cemit\\u00E9rios.\",\"Benefits\":\"\\u002B1 Fortitude ou Obl\\u00EDvio; Es\\u00F4fago de Ferro 3; Ref\\u00FAgio 1; especializa\\u00E7\\u00E3o em Ocultismo (Rituais Tumulares) ou Medicina (texto livre).\",\"Cost\":\"Predador Manifesto 2.\",\"Disciplines\":[\"Fortitude\",\"Obl\\u00EDvio\"],\"Specializations\":[{\"Skill\":\"Ocultismo\",\"Text\":\"Rituais Tumulares\",\"IsFreeText\":false,\"Label\":\"Ocultismo (Rituais Tumulares)\"},{\"Skill\":\"Medicina\",\"Text\":\"especializa\\u00E7\\u00E3o a informar\",\"IsFreeText\":true,\"Label\":\"Medicina (especializa\\u00E7\\u00E3o a informar)\"}],\"FixedGrants\":[{\"Name\":\"Esofago de Ferro\",\"Category\":3},{\"Name\":\"Refugio\",\"Category\":3},{\"Name\":\"Predador Manifesto\",\"Category\":4}],\"FixedPoints\":[3,1,2],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":0,\"BloodDelta\":0}", "Ladrão de Sepultura" },
                    { "predator-osiris", "{\"Id\":\"predator-osiris\",\"Name\":\"Os\\u00EDris\",\"Description\":\"Algu\\u00E9m com poder de multid\\u00E3o (l\\u00EDderes de culto, DJs, celebridades) que bebe o sangue de f\\u00E3s fan\\u00E1ticos e adoradores em transe.\",\"Benefits\":\"\\u002B1 Feiti\\u00E7aria de Sangue (s\\u00F3 Tremere) ou Presen\\u00E7a; 3 pontos em Fama/Rebanho; especializa\\u00E7\\u00E3o em Ocultismo (tradi\\u00E7\\u00E3o espec\\u00EDfica) ou Performance (cena espec\\u00EDfica).\",\"Cost\":\"2 pontos entre Defeitos Inimigos ou Defeitos M\\u00EDticos.\",\"Disciplines\":[\"Feiti\\u00E7aria de Sangue\",\"Presen\\u00E7a\"],\"Specializations\":[{\"Skill\":\"Ocultismo\",\"Text\":\"tradi\\u00E7\\u00E3o espec\\u00EDfica\",\"IsFreeText\":true,\"Label\":\"Ocultismo (tradi\\u00E7\\u00E3o espec\\u00EDfica)\"},{\"Skill\":\"Performance\",\"Text\":\"cena espec\\u00EDfica\",\"IsFreeText\":true,\"Label\":\"Performance (cena espec\\u00EDfica)\"}],\"FixedGrants\":[],\"FixedPoints\":[],\"AllocationOptions\":[{\"Name\":\"Fama\",\"Category\":3},{\"Name\":\"Rebanho\",\"Category\":3}],\"AllocationPoints\":3,\"FlawOptions\":[{\"Name\":\"Inimigo\",\"Category\":4},{\"Name\":\"Perdicao Folclorica\",\"Category\":4},{\"Name\":\"Tabu Folclorico\",\"Category\":4},{\"Name\":\"Estigma\",\"Category\":4},{\"Name\":\"Isca de Estaca\",\"Category\":4}],\"FlawPoints\":2,\"SplitFlaws\":true,\"HumanityDelta\":0,\"BloodDelta\":0}", "Osíris" },
                    { "predator-roadside", "{\"Id\":\"predator-roadside\",\"Name\":\"Assassino na Estrada\",\"Description\":\"Um viajante focado em ca\\u00E7ar pessoas em movimento ou distantes da lei, como mochileiros e motoristas solit\\u00E1rios.\",\"Benefits\":\"\\u002B1 Fortitude ou Proteanismo; \\u002B2 Rebanho Itinerante; especializa\\u00E7\\u00E3o em Sobreviv\\u00EAncia (Estradas) ou Investiga\\u00E7\\u00E3o (Grafite Vamp\\u00EDrico).\",\"Cost\":\"Exclus\\u00E3o de Presas 1 (locais sedent\\u00E1rios).\",\"Disciplines\":[\"Fortitude\",\"Proteanismo\"],\"Specializations\":[{\"Skill\":\"Sobreviv\\u00EAncia\",\"Text\":\"Estradas\",\"IsFreeText\":false,\"Label\":\"Sobreviv\\u00EAncia (Estradas)\"},{\"Skill\":\"Investiga\\u00E7\\u00E3o\",\"Text\":\"Grafite Vamp\\u00EDrico\",\"IsFreeText\":false,\"Label\":\"Investiga\\u00E7\\u00E3o (Grafite Vamp\\u00EDrico)\"}],\"FixedGrants\":[{\"Name\":\"Rebanho Itinerante\",\"Category\":3},{\"Name\":\"Exclusao de Presas (locais sedentarios)\",\"Category\":4}],\"FixedPoints\":[2,1],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":0,\"BloodDelta\":0}", "Assassino na Estrada" },
                    { "predator-sandman", "{\"Id\":\"predator-sandman\",\"Name\":\"Sandman\",\"Description\":\"Extrema furtividade e discri\\u00E7\\u00E3o noturna; esgueira-se pelas casas e suga sangue de pessoas profundamente adormecidas sem deixar rastro.\",\"Benefits\":\"\\u002B1 Ausp\\u00EDcios ou Ofusca\\u00E7\\u00E3o; \\u002B1 Recursos; especializa\\u00E7\\u00E3o em Medicina (Anest\\u00E9sicos) ou Furtividade (Invas\\u00E3o).\",\"Cost\":\"N\\u00E3o h\\u00E1 custo negativo.\",\"Disciplines\":[\"Ausp\\u00EDcios\",\"Ofusca\\u00E7\\u00E3o\"],\"Specializations\":[{\"Skill\":\"Medicina\",\"Text\":\"Anest\\u00E9sicos\",\"IsFreeText\":false,\"Label\":\"Medicina (Anest\\u00E9sicos)\"},{\"Skill\":\"Furtividade\",\"Text\":\"Invas\\u00E3o\",\"IsFreeText\":false,\"Label\":\"Furtividade (Invas\\u00E3o)\"}],\"FixedGrants\":[{\"Name\":\"Recursos\",\"Category\":3}],\"FixedPoints\":[1],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":0,\"BloodDelta\":0}", "Sandman" },
                    { "predator-scene-queen", "{\"Id\":\"predator-scene-queen\",\"Name\":\"Scene Queen\",\"Description\":\"Um socialite que usa seu prest\\u00EDgio dentro de uma subcultura espec\\u00EDfica para encantar as v\\u00EDtimas como um colega influente da mesma cena.\",\"Benefits\":\"\\u002B1 Domina\\u00E7\\u00E3o ou Pot\\u00EAncia; \\u002B1 Contatos; \\u002B1 Fama; especializa\\u00E7\\u00E3o em Etiqueta, Lideran\\u00E7a ou Manha (Cena Espec\\u00EDfica).\",\"Cost\":\"Rejeitado 1 OU Exclus\\u00E3o de Presa 1 (Subcultura diferente).\",\"Disciplines\":[\"Domina\\u00E7\\u00E3o\",\"Pot\\u00EAncia\"],\"Specializations\":[{\"Skill\":\"Etiqueta\",\"Text\":\"Cena Espec\\u00EDfica\",\"IsFreeText\":true,\"Label\":\"Etiqueta (Cena Espec\\u00EDfica)\"},{\"Skill\":\"Lideran\\u00E7a\",\"Text\":\"Cena Espec\\u00EDfica\",\"IsFreeText\":true,\"Label\":\"Lideran\\u00E7a (Cena Espec\\u00EDfica)\"},{\"Skill\":\"Manha\",\"Text\":\"Cena Espec\\u00EDfica\",\"IsFreeText\":true,\"Label\":\"Manha (Cena Espec\\u00EDfica)\"}],\"FixedGrants\":[{\"Name\":\"Contatos\",\"Category\":3},{\"Name\":\"Fama\",\"Category\":3}],\"FixedPoints\":[1,1],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[{\"Name\":\"Rejeitado\",\"Category\":4},{\"Name\":\"Exclusao de Presa (Subcultura diferente)\",\"Category\":4}],\"FlawPoints\":1,\"SplitFlaws\":false,\"HumanityDelta\":0,\"BloodDelta\":0}", "Scene Queen" },
                    { "predator-siren", "{\"Id\":\"predator-siren\",\"Name\":\"Sereia\",\"Description\":\"O predador focado em lux\\u00FAria que ca\\u00E7a pessoas fingindo desejo sexual, alimentando-se enquanto a v\\u00EDtima est\\u00E1 seduzida.\",\"Benefits\":\"\\u002B1 Fortitude ou Presen\\u00E7a; Visual Belo 2; especializa\\u00E7\\u00E3o em Persuas\\u00E3o (Sedu\\u00E7\\u00E3o) ou Subterf\\u00FAgio (Sedu\\u00E7\\u00E3o).\",\"Cost\":\"Inimigo 1 (ex-amante abandonado ou parceiro ciumento).\",\"Disciplines\":[\"Fortitude\",\"Presen\\u00E7a\"],\"Specializations\":[{\"Skill\":\"Persuas\\u00E3o\",\"Text\":\"Sedu\\u00E7\\u00E3o\",\"IsFreeText\":false,\"Label\":\"Persuas\\u00E3o (Sedu\\u00E7\\u00E3o)\"},{\"Skill\":\"Subterf\\u00FAgio\",\"Text\":\"Sedu\\u00E7\\u00E3o\",\"IsFreeText\":false,\"Label\":\"Subterf\\u00FAgio (Sedu\\u00E7\\u00E3o)\"}],\"FixedGrants\":[{\"Name\":\"Visual Belo\",\"Category\":3},{\"Name\":\"Inimigo (ex-amante ou parceiro ciumento)\",\"Category\":4}],\"FixedPoints\":[2,1],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":0,\"BloodDelta\":0}", "Sereia" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-visual-belo' WHERE \"Id\" = 'advantage-looks-beautiful';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-deslumbrante' WHERE \"Id\" = 'advantage-looks-stunning';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-feio' WHERE \"Id\" = 'flaw-looks-ugly';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-repulsivo' WHERE \"Id\" = 'flaw-looks-repulsive';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-refugio' WHERE \"Id\" = 'advantage-haven';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-arsenal-oculto' WHERE \"Id\" = 'advantage-haven-hidden-armory';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-vigilancia' WHERE \"Id\" = 'advantage-haven-watchmen';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-luxo' WHERE \"Id\" = 'advantage-haven-luxury';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-nenhum-refugio' WHERE \"Id\" = 'flaw-no-haven';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-assombrado' WHERE \"Id\" = 'flaw-haven-haunted';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-comprometido' WHERE \"Id\" = 'flaw-haven-compromised';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-recursos' WHERE \"Id\" = 'advantage-resources';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-destituido' WHERE \"Id\" = 'flaw-destitute';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-sabujo-de-sangue' WHERE \"Id\" = 'advantage-bloodhound';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-esofago-de-ferro' WHERE \"Id\" = 'advantage-iron-gullet';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-exclusao-de-presa' WHERE \"Id\" = 'flaw-prey-exclusion';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-sede-do-matusalem' WHERE \"Id\" = 'flaw-methuselahs-thirst';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-fazendeiro' WHERE \"Id\" = 'flaw-farmer';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-organivoro' WHERE \"Id\" = 'flaw-organovore';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-mawla' WHERE \"Id\" = 'advantage-mawla';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-status' WHERE \"Id\" = 'advantage-status';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-adversario' WHERE \"Id\" = 'flaw-adversary';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-suspeito' WHERE \"Id\" = 'flaw-suspect';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-segregado' WHERE \"Id\" = 'flaw-shunned';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-vivendo-no-passado' WHERE \"Id\" = 'flaw-living-in-the-past';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-arcaico' WHERE \"Id\" = 'flaw-archaic';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-ingerir-comida' WHERE \"Id\" = 'advantage-eat-food';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-perdicao-folclorica' WHERE \"Id\" = 'flaw-folkloric-bane';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-tabu-folclorico' WHERE \"Id\" = 'flaw-folkloric-block';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-estigma' WHERE \"Id\" = 'flaw-stigmata';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-isca-de-estaca' WHERE \"Id\" = 'flaw-stake-bait';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-mascara' WHERE \"Id\" = 'advantage-mask';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-cadaver-identificado' WHERE \"Id\" = 'flaw-known-corpse';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-corpo-frio-identificado' WHERE \"Id\" = 'flaw-known-blankbody';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-linguistica' WHERE \"Id\" = 'advantage-linguistics';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-analfabeto' WHERE \"Id\" = 'flaw-illiterate';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-lacaios' WHERE \"Id\" = 'advantage-retainers';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-aliados' WHERE \"Id\" = 'advantage-allies';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-contatos' WHERE \"Id\" = 'advantage-contacts';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-rebanho' WHERE \"Id\" = 'advantage-herd';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-perseguidores' WHERE \"Id\" = 'flaw-stalkers';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-inimigo' WHERE \"Id\" = 'flaw-enemy';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'catalog-predador-manifesto' WHERE \"Id\" = 'flaw-obvious-predator';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-bahari-1' WHERE \"Id\" = 'loresheet-bahari-1';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-bahari-2' WHERE \"Id\" = 'loresheet-bahari-2';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-bahari-3' WHERE \"Id\" = 'loresheet-bahari-3';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-bahari-4' WHERE \"Id\" = 'loresheet-bahari-4';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-bahari-5' WHERE \"Id\" = 'loresheet-bahari-5';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-theo-bell-1' WHERE \"Id\" = 'loresheet-theo-bell-1';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-theo-bell-2' WHERE \"Id\" = 'loresheet-theo-bell-2';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-theo-bell-3' WHERE \"Id\" = 'loresheet-theo-bell-3';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-theo-bell-4' WHERE \"Id\" = 'loresheet-theo-bell-4';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-theo-bell-5' WHERE \"Id\" = 'loresheet-theo-bell-5';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-heresia-cainita-1' WHERE \"Id\" = 'loresheet-cainite-heresy-1';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-heresia-cainita-2' WHERE \"Id\" = 'loresheet-cainite-heresy-2';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-heresia-cainita-3' WHERE \"Id\" = 'loresheet-cainite-heresy-3';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-heresia-cainita-4' WHERE \"Id\" = 'loresheet-cainite-heresy-4';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-heresia-cainita-5' WHERE \"Id\" = 'loresheet-cainite-heresy-5';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-carna-1' WHERE \"Id\" = 'loresheet-carna-1';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-carna-2' WHERE \"Id\" = 'loresheet-carna-2';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-carna-3' WHERE \"Id\" = 'loresheet-carna-3';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-carna-4' WHERE \"Id\" = 'loresheet-carna-4';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'lore-carna-5' WHERE \"Id\" = 'loresheet-carna-5';");
            migrationBuilder.Sql("UPDATE \"AdvantageCatalog\" SET \"Id\" = 'adv-custom-' || substr(\"Id\", instr(\"Id\", '-custom-') + 8) WHERE \"Id\" LIKE '%-custom-%';");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "predator-alleycat");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "predator-bagger");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "predator-blood-leech");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "predator-cleaver");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "predator-consensualist");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "predator-extortionist");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "predator-farmer");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "predator-graverobber");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "predator-osiris");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "predator-roadside");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "predator-sandman");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "predator-scene-queen");

            migrationBuilder.DeleteData(
                table: "PredatorCatalog",
                keyColumn: "Id",
                keyValue: "predator-siren");

            migrationBuilder.InsertData(
                table: "PredatorCatalog",
                columns: new[] { "Id", "Definition", "Name" },
                values: new object[,]
                {
                    { "alleycat", "{\"Id\":\"alleycat\",\"Name\":\"Vira-lata\",\"Description\":\"Um ca\\u00E7ador agressivo que ataca suas v\\u00EDtimas nas ruas, seja disfar\\u00E7ando a ca\\u00E7ada como um assalto ou dominando sua presa pela for\\u00E7a.\",\"Benefits\":\"\\u002B1 Celeridade ou Pot\\u00EAncia; \\u002B3 Contatos Criminosos; especializa\\u00E7\\u00E3o em Intimida\\u00E7\\u00E3o (Assalto \\u00E0 M\\u00E3o Armada) ou Briga (Agarramento).\",\"Cost\":\"Perde 1 ponto de Humanidade.\",\"Disciplines\":[\"Celeridade\",\"Pot\\u00EAncia\"],\"Specializations\":[{\"Skill\":\"Intimida\\u00E7\\u00E3o\",\"Text\":\"Assalto \\u00E0 M\\u00E3o Armada\",\"IsFreeText\":false,\"Label\":\"Intimida\\u00E7\\u00E3o (Assalto \\u00E0 M\\u00E3o Armada)\"},{\"Skill\":\"Briga\",\"Text\":\"Agarramento\",\"IsFreeText\":false,\"Label\":\"Briga (Agarramento)\"}],\"FixedGrants\":[{\"Name\":\"Contatos Criminosos\",\"Category\":3}],\"FixedPoints\":[3],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":-1,\"BloodDelta\":0}", "Vira-lata" },
                    { "bagger", "{\"Id\":\"bagger\",\"Name\":\"Sacoleiro\",\"Description\":\"Evita viol\\u00EAncia de ca\\u00E7a real, adquirindo sangue empacotado em hospitais ou contrabandeando do mercado negro. Ventrue n\\u00E3o pode escolher.\",\"Benefits\":\"\\u002B1 Ofusca\\u00E7\\u00E3o, Obl\\u00EDvio ou Feiti\\u00E7aria de Sangue (s\\u00F3 Tremere); Es\\u00F4fago de Ferro 3; especializa\\u00E7\\u00E3o em Ladroagem (Abrir Fechaduras) ou Manha (Mercado Negro).\",\"Cost\":\"Inimigo 2 (cobrando d\\u00EDvidas ou banindo-o das ruas).\",\"Disciplines\":[\"Ofusca\\u00E7\\u00E3o\",\"Obl\\u00EDvio\",\"Feiti\\u00E7aria de Sangue\"],\"Specializations\":[{\"Skill\":\"Ladroagem\",\"Text\":\"Abrir Fechaduras\",\"IsFreeText\":false,\"Label\":\"Ladroagem (Abrir Fechaduras)\"},{\"Skill\":\"Manha\",\"Text\":\"Mercado Negro\",\"IsFreeText\":false,\"Label\":\"Manha (Mercado Negro)\"}],\"FixedGrants\":[{\"Name\":\"Esofago de Ferro\",\"Category\":3},{\"Name\":\"Inimigo (dividas ou banimento)\",\"Category\":4}],\"FixedPoints\":[3,2],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":0,\"BloodDelta\":0}", "Sacoleiro" },
                    { "blood-leech", "{\"Id\":\"blood-leech\",\"Name\":\"Sanguessuga\",\"Description\":\"Um canibal de Membros e Diablerista ocasional; detesta se alimentar de humanos e busca sangue de outros vampiros.\",\"Benefits\":\"\\u002B1 Celeridade ou Proteanismo; \\u002B1 Pot\\u00EAncia de Sangue; especializa\\u00E7\\u00E3o em Briga (Membros) ou Furtividade (Contra Membros).\",\"Cost\":\"-1 Humanidade; Segredo Sombrio 2 (Diablerista) OU Segregado 2; Exclus\\u00E3o de Presa 2 (Mortais).\",\"Disciplines\":[\"Celeridade\",\"Proteanismo\"],\"Specializations\":[{\"Skill\":\"Briga\",\"Text\":\"Membros\",\"IsFreeText\":false,\"Label\":\"Briga (Membros)\"},{\"Skill\":\"Furtividade\",\"Text\":\"Contra Membros\",\"IsFreeText\":false,\"Label\":\"Furtividade (Contra Membros)\"}],\"FixedGrants\":[{\"Name\":\"Exclusao de Presa (Mortais)\",\"Category\":4}],\"FixedPoints\":[2],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[{\"Name\":\"Segredo Sombrio (Diablerista)\",\"Category\":4},{\"Name\":\"Segregado\",\"Category\":4}],\"FlawPoints\":2,\"SplitFlaws\":false,\"HumanityDelta\":-1,\"BloodDelta\":1}", "Sanguessuga" },
                    { "cleaver", "{\"Id\":\"cleaver\",\"Name\":\"Trinchador\",\"Description\":\"Um parasita familiar; esconde a sua natureza bestial de seus amigos e familiares mundanos, enquanto se alimenta secretamente deles no conv\\u00EDvio di\\u00E1rio.\",\"Benefits\":\"\\u002B1 Domina\\u00E7\\u00E3o ou Animalismo; \\u002B2 Rebanho; especializa\\u00E7\\u00E3o em Persuas\\u00E3o (Gaslighting) ou Subterf\\u00FAgio (Encobrimento).\",\"Cost\":\"Segredo Sombrio 1: Trinchador.\",\"Disciplines\":[\"Domina\\u00E7\\u00E3o\",\"Animalismo\"],\"Specializations\":[{\"Skill\":\"Persuas\\u00E3o\",\"Text\":\"Gaslighting\",\"IsFreeText\":false,\"Label\":\"Persuas\\u00E3o (Gaslighting)\"},{\"Skill\":\"Subterf\\u00FAgio\",\"Text\":\"Encobrimento\",\"IsFreeText\":false,\"Label\":\"Subterf\\u00FAgio (Encobrimento)\"}],\"FixedGrants\":[{\"Name\":\"Rebanho\",\"Category\":3},{\"Name\":\"Segredo Sombrio (Trinchador)\",\"Category\":4}],\"FixedPoints\":[2,1],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":0,\"BloodDelta\":0}", "Trinchador" },
                    { "consensualist", "{\"Id\":\"consensualist\",\"Name\":\"Consensualista\",\"Description\":\"S\\u00F3 se alimenta do sangue de pessoas que aceitam e oferecem livremente; pode dizer a verdade ou se disfar\\u00E7ar de trabalhador de caridade/adepto de cena fetiche vamp\\u00EDrica.\",\"Benefits\":\"\\u002B1 Ausp\\u00EDcios ou Fortitude; \\u002B1 Humanidade; especializa\\u00E7\\u00E3o em Medicina (Flebotomia) ou Persuas\\u00E3o (Bolsas).\",\"Cost\":\"Segredo Sombrio 1: Quebrador da M\\u00E1scara; Exclus\\u00E3o de Presa 1 (sem consentimento).\",\"Disciplines\":[\"Ausp\\u00EDcios\",\"Fortitude\"],\"Specializations\":[{\"Skill\":\"Medicina\",\"Text\":\"Flebotomia\",\"IsFreeText\":false,\"Label\":\"Medicina (Flebotomia)\"},{\"Skill\":\"Persuas\\u00E3o\",\"Text\":\"Bolsas\",\"IsFreeText\":false,\"Label\":\"Persuas\\u00E3o (Bolsas)\"}],\"FixedGrants\":[{\"Name\":\"Segredo Sombrio (Quebrador da Mascara)\",\"Category\":4},{\"Name\":\"Exclusao de Presa (sem consentimento)\",\"Category\":4}],\"FixedPoints\":[1,1],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":1,\"BloodDelta\":0}", "Consensualista" },
                    { "extortionist", "{\"Id\":\"extortionist\",\"Name\":\"Chantagista\",\"Description\":\"Adquire sangue mediante amea\\u00E7as e extors\\u00E3o.\",\"Benefits\":\"\\u002B1 Domina\\u00E7\\u00E3o ou Pot\\u00EAncia; 3 pontos entre Contatos e Recursos; especializa\\u00E7\\u00E3o em Intimida\\u00E7\\u00E3o (Coer\\u00E7\\u00E3o) ou Ladroagem (Seguran\\u00E7a).\",\"Cost\":\"Inimigo 2 (a v\\u00EDtima extorquida ou a pol\\u00EDcia).\",\"Disciplines\":[\"Domina\\u00E7\\u00E3o\",\"Pot\\u00EAncia\"],\"Specializations\":[{\"Skill\":\"Intimida\\u00E7\\u00E3o\",\"Text\":\"Coer\\u00E7\\u00E3o\",\"IsFreeText\":false,\"Label\":\"Intimida\\u00E7\\u00E3o (Coer\\u00E7\\u00E3o)\"},{\"Skill\":\"Ladroagem\",\"Text\":\"Seguran\\u00E7a\",\"IsFreeText\":false,\"Label\":\"Ladroagem (Seguran\\u00E7a)\"}],\"FixedGrants\":[{\"Name\":\"Inimigo (extorsao)\",\"Category\":4}],\"FixedPoints\":[2],\"AllocationOptions\":[{\"Name\":\"Contatos\",\"Category\":3},{\"Name\":\"Recursos\",\"Category\":3}],\"AllocationPoints\":3,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":0,\"BloodDelta\":0}", "Chantagista" },
                    { "farmer", "{\"Id\":\"farmer\",\"Name\":\"Fazendeiro\",\"Description\":\"N\\u00E3o ataca seres humanos e foca sua alimenta\\u00E7\\u00E3o em animais. Ventrue e Membros com Pot\\u00EAncia de Sangue 3\\u002B n\\u00E3o podem escolher.\",\"Benefits\":\"\\u002B1 Animalismo ou Proteanismo; \\u002B1 Humanidade; especializa\\u00E7\\u00E3o em Empatia com Animais (escolha um) ou Sobreviv\\u00EAncia (Ca\\u00E7a).\",\"Cost\":\"Defeito de Alimenta\\u00E7\\u00E3o 2: Fazendeiro.\",\"Disciplines\":[\"Animalismo\",\"Proteanismo\"],\"Specializations\":[{\"Skill\":\"Empatia com Animais\",\"Text\":\"animal a informar\",\"IsFreeText\":true,\"Label\":\"Empatia com Animais (animal a informar)\"},{\"Skill\":\"Sobreviv\\u00EAncia\",\"Text\":\"Ca\\u00E7a\",\"IsFreeText\":false,\"Label\":\"Sobreviv\\u00EAncia (Ca\\u00E7a)\"}],\"FixedGrants\":[{\"Name\":\"Fazendeiro\",\"Category\":4}],\"FixedPoints\":[2],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":1,\"BloodDelta\":0}", "Fazendeiro" },
                    { "graverobber", "{\"Id\":\"graverobber\",\"Name\":\"Ladr\\u00E3o de Sepultura\",\"Description\":\"Restringe-se a ca\\u00E7ar presas nos cemit\\u00E9rios.\",\"Benefits\":\"\\u002B1 Fortitude ou Obl\\u00EDvio; Es\\u00F4fago de Ferro 3; Ref\\u00FAgio 1; especializa\\u00E7\\u00E3o em Ocultismo (Rituais Tumulares) ou Medicina (texto livre).\",\"Cost\":\"Predador Manifesto 2.\",\"Disciplines\":[\"Fortitude\",\"Obl\\u00EDvio\"],\"Specializations\":[{\"Skill\":\"Ocultismo\",\"Text\":\"Rituais Tumulares\",\"IsFreeText\":false,\"Label\":\"Ocultismo (Rituais Tumulares)\"},{\"Skill\":\"Medicina\",\"Text\":\"especializa\\u00E7\\u00E3o a informar\",\"IsFreeText\":true,\"Label\":\"Medicina (especializa\\u00E7\\u00E3o a informar)\"}],\"FixedGrants\":[{\"Name\":\"Esofago de Ferro\",\"Category\":3},{\"Name\":\"Refugio\",\"Category\":3},{\"Name\":\"Predador Manifesto\",\"Category\":4}],\"FixedPoints\":[3,1,2],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":0,\"BloodDelta\":0}", "Ladrão de Sepultura" },
                    { "osiris", "{\"Id\":\"osiris\",\"Name\":\"Os\\u00EDris\",\"Description\":\"Algu\\u00E9m com poder de multid\\u00E3o (l\\u00EDderes de culto, DJs, celebridades) que bebe o sangue de f\\u00E3s fan\\u00E1ticos e adoradores em transe.\",\"Benefits\":\"\\u002B1 Feiti\\u00E7aria de Sangue (s\\u00F3 Tremere) ou Presen\\u00E7a; 3 pontos em Fama/Rebanho; especializa\\u00E7\\u00E3o em Ocultismo (tradi\\u00E7\\u00E3o espec\\u00EDfica) ou Performance (cena espec\\u00EDfica).\",\"Cost\":\"2 pontos entre Defeitos Inimigos ou Defeitos M\\u00EDticos.\",\"Disciplines\":[\"Feiti\\u00E7aria de Sangue\",\"Presen\\u00E7a\"],\"Specializations\":[{\"Skill\":\"Ocultismo\",\"Text\":\"tradi\\u00E7\\u00E3o espec\\u00EDfica\",\"IsFreeText\":true,\"Label\":\"Ocultismo (tradi\\u00E7\\u00E3o espec\\u00EDfica)\"},{\"Skill\":\"Performance\",\"Text\":\"cena espec\\u00EDfica\",\"IsFreeText\":true,\"Label\":\"Performance (cena espec\\u00EDfica)\"}],\"FixedGrants\":[],\"FixedPoints\":[],\"AllocationOptions\":[{\"Name\":\"Fama\",\"Category\":3},{\"Name\":\"Rebanho\",\"Category\":3}],\"AllocationPoints\":3,\"FlawOptions\":[{\"Name\":\"Inimigo\",\"Category\":4},{\"Name\":\"Perdicao Folclorica\",\"Category\":4},{\"Name\":\"Tabu Folclorico\",\"Category\":4},{\"Name\":\"Estigma\",\"Category\":4},{\"Name\":\"Isca de Estaca\",\"Category\":4}],\"FlawPoints\":2,\"SplitFlaws\":true,\"HumanityDelta\":0,\"BloodDelta\":0}", "Osíris" },
                    { "roadside", "{\"Id\":\"roadside\",\"Name\":\"Assassino na Estrada\",\"Description\":\"Um viajante focado em ca\\u00E7ar pessoas em movimento ou distantes da lei, como mochileiros e motoristas solit\\u00E1rios.\",\"Benefits\":\"\\u002B1 Fortitude ou Proteanismo; \\u002B2 Rebanho Itinerante; especializa\\u00E7\\u00E3o em Sobreviv\\u00EAncia (Estradas) ou Investiga\\u00E7\\u00E3o (Grafite Vamp\\u00EDrico).\",\"Cost\":\"Exclus\\u00E3o de Presas 1 (locais sedent\\u00E1rios).\",\"Disciplines\":[\"Fortitude\",\"Proteanismo\"],\"Specializations\":[{\"Skill\":\"Sobreviv\\u00EAncia\",\"Text\":\"Estradas\",\"IsFreeText\":false,\"Label\":\"Sobreviv\\u00EAncia (Estradas)\"},{\"Skill\":\"Investiga\\u00E7\\u00E3o\",\"Text\":\"Grafite Vamp\\u00EDrico\",\"IsFreeText\":false,\"Label\":\"Investiga\\u00E7\\u00E3o (Grafite Vamp\\u00EDrico)\"}],\"FixedGrants\":[{\"Name\":\"Rebanho Itinerante\",\"Category\":3},{\"Name\":\"Exclusao de Presas (locais sedentarios)\",\"Category\":4}],\"FixedPoints\":[2,1],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":0,\"BloodDelta\":0}", "Assassino na Estrada" },
                    { "sandman", "{\"Id\":\"sandman\",\"Name\":\"Sandman\",\"Description\":\"Extrema furtividade e discri\\u00E7\\u00E3o noturna; esgueira-se pelas casas e suga sangue de pessoas profundamente adormecidas sem deixar rastro.\",\"Benefits\":\"\\u002B1 Ausp\\u00EDcios ou Ofusca\\u00E7\\u00E3o; \\u002B1 Recursos; especializa\\u00E7\\u00E3o em Medicina (Anest\\u00E9sicos) ou Furtividade (Invas\\u00E3o).\",\"Cost\":\"N\\u00E3o h\\u00E1 custo negativo.\",\"Disciplines\":[\"Ausp\\u00EDcios\",\"Ofusca\\u00E7\\u00E3o\"],\"Specializations\":[{\"Skill\":\"Medicina\",\"Text\":\"Anest\\u00E9sicos\",\"IsFreeText\":false,\"Label\":\"Medicina (Anest\\u00E9sicos)\"},{\"Skill\":\"Furtividade\",\"Text\":\"Invas\\u00E3o\",\"IsFreeText\":false,\"Label\":\"Furtividade (Invas\\u00E3o)\"}],\"FixedGrants\":[{\"Name\":\"Recursos\",\"Category\":3}],\"FixedPoints\":[1],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":0,\"BloodDelta\":0}", "Sandman" },
                    { "scene-queen", "{\"Id\":\"scene-queen\",\"Name\":\"Scene Queen\",\"Description\":\"Um socialite que usa seu prest\\u00EDgio dentro de uma subcultura espec\\u00EDfica para encantar as v\\u00EDtimas como um colega influente da mesma cena.\",\"Benefits\":\"\\u002B1 Domina\\u00E7\\u00E3o ou Pot\\u00EAncia; \\u002B1 Contatos; \\u002B1 Fama; especializa\\u00E7\\u00E3o em Etiqueta, Lideran\\u00E7a ou Manha (Cena Espec\\u00EDfica).\",\"Cost\":\"Rejeitado 1 OU Exclus\\u00E3o de Presa 1 (Subcultura diferente).\",\"Disciplines\":[\"Domina\\u00E7\\u00E3o\",\"Pot\\u00EAncia\"],\"Specializations\":[{\"Skill\":\"Etiqueta\",\"Text\":\"Cena Espec\\u00EDfica\",\"IsFreeText\":true,\"Label\":\"Etiqueta (Cena Espec\\u00EDfica)\"},{\"Skill\":\"Lideran\\u00E7a\",\"Text\":\"Cena Espec\\u00EDfica\",\"IsFreeText\":true,\"Label\":\"Lideran\\u00E7a (Cena Espec\\u00EDfica)\"},{\"Skill\":\"Manha\",\"Text\":\"Cena Espec\\u00EDfica\",\"IsFreeText\":true,\"Label\":\"Manha (Cena Espec\\u00EDfica)\"}],\"FixedGrants\":[{\"Name\":\"Contatos\",\"Category\":3},{\"Name\":\"Fama\",\"Category\":3}],\"FixedPoints\":[1,1],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[{\"Name\":\"Rejeitado\",\"Category\":4},{\"Name\":\"Exclusao de Presa (Subcultura diferente)\",\"Category\":4}],\"FlawPoints\":1,\"SplitFlaws\":false,\"HumanityDelta\":0,\"BloodDelta\":0}", "Scene Queen" },
                    { "siren", "{\"Id\":\"siren\",\"Name\":\"Sereia\",\"Description\":\"O predador focado em lux\\u00FAria que ca\\u00E7a pessoas fingindo desejo sexual, alimentando-se enquanto a v\\u00EDtima est\\u00E1 seduzida.\",\"Benefits\":\"\\u002B1 Fortitude ou Presen\\u00E7a; Visual Belo 2; especializa\\u00E7\\u00E3o em Persuas\\u00E3o (Sedu\\u00E7\\u00E3o) ou Subterf\\u00FAgio (Sedu\\u00E7\\u00E3o).\",\"Cost\":\"Inimigo 1 (ex-amante abandonado ou parceiro ciumento).\",\"Disciplines\":[\"Fortitude\",\"Presen\\u00E7a\"],\"Specializations\":[{\"Skill\":\"Persuas\\u00E3o\",\"Text\":\"Sedu\\u00E7\\u00E3o\",\"IsFreeText\":false,\"Label\":\"Persuas\\u00E3o (Sedu\\u00E7\\u00E3o)\"},{\"Skill\":\"Subterf\\u00FAgio\",\"Text\":\"Sedu\\u00E7\\u00E3o\",\"IsFreeText\":false,\"Label\":\"Subterf\\u00FAgio (Sedu\\u00E7\\u00E3o)\"}],\"FixedGrants\":[{\"Name\":\"Visual Belo\",\"Category\":3},{\"Name\":\"Inimigo (ex-amante ou parceiro ciumento)\",\"Category\":4}],\"FixedPoints\":[2,1],\"AllocationOptions\":[],\"AllocationPoints\":0,\"FlawOptions\":[],\"FlawPoints\":0,\"SplitFlaws\":false,\"HumanityDelta\":0,\"BloodDelta\":0}", "Sereia" }
                });
        }
    }
}
