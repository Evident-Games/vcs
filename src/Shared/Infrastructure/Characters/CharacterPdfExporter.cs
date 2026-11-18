using PdfSharp.Fonts;
using Game.Application.Catalogs;
using Game.Application.Characters;
using Game.Rules.V5.Validation;

namespace Game.Infrastructure.Characters;

public sealed class CharacterPdfExporter(IDisciplineCatalog powers, ISkillCatalog skills) : ICharacterPdfExporter
{
    static CharacterPdfExporter() => GlobalFontSettings.UseWindowsFontsUnderWindows = true;

    public async Task ExportAsync(CharacterDto character, string path, IReadOnlyList<PdfSection> sections,
        CancellationToken cancellationToken = default)
    {
        var issues = CharacterRules.Validate(character.Details, character.Traits);
        if (issues.Count != 0) throw new CharacterValidationException(issues);
        if (!character.Details.IsNpc && sections.Count == 0)
            throw new InvalidDataException("Selecione ao menos uma secao para exportar.");
        var catalog = await skills.ListAsync(cancellationToken);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await Task.Run(() =>
            {
                using var writer = new PdfSheetWriter(character, powers, catalog, cancellationToken);
                writer.Write(sections);
                writer.Save(temporary);
            }, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
