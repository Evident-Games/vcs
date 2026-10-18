using VCS.Application.Characters;

namespace VCS.Application.Abstractions;

public sealed record PdfSection(string Key, string Title);

public interface ICharacterPdfExporter
{
    Task ExportAsync(CharacterDto character, string path, IReadOnlyList<PdfSection> sections,
        CancellationToken cancellationToken = default);
}
