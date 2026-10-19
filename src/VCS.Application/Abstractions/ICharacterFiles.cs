using VCS.Application.Characters;

namespace VCS.Application.Abstractions;

public interface ICharacterFiles
{
    Task ExportAsync(CharacterDto character, string path, CancellationToken cancellationToken = default);
    Task<CharacterDto> ReadAsync(string path, CancellationToken cancellationToken = default);
}

public interface IDatabaseMaintenance
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task BackupAsync(string path, CancellationToken cancellationToken = default);
    Task<string> RestoreAsync(string path, CancellationToken cancellationToken = default);
}
