using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameBloodCheckAndResolve : Migration
    {
        // Rule text fixes: "Teste de Incitar o Sangue" became "checagem de sangue" and "Perseverança" became "Determinação".
        // Sheets keep a copy of each power's cost and dice pool, stored as JSON where non-ASCII letters may be escaped.
        private static readonly (string From, string To)[] Replacements =
        [
            ("Testes de Incitar o Sangue", "checagens de sangue"),
            ("Teste de Incitar o Sangue", "checagem de sangue"),
            ("Teste de Incitar", "checagem de sangue"),
            ("Perseverança", "Determinação"),
            ("Perseveran\\u00E7a", "Determina\\u00E7\\u00E3o"),
            ("Perseveran\\u00e7a", "Determina\\u00e7\\u00e3o")
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (from, to) in Replacements)
            {
                var (a, b) = (from.Replace("'", "''"), to.Replace("'", "''"));
                migrationBuilder.Sql($"UPDATE \"CharacterTraits\" SET \"Power\" = REPLACE(\"Power\", '{a}', '{b}') WHERE \"Power\" LIKE '%{a}%';");
                migrationBuilder.Sql($"UPDATE \"PowerCatalog\" SET \"Cost\" = REPLACE(\"Cost\", '{a}', '{b}'), \"DicePool\" = REPLACE(\"DicePool\", '{a}', '{b}'), \"Summary\" = REPLACE(\"Summary\", '{a}', '{b}');");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
