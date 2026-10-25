namespace VCS.Application.Characters;

public interface ICharacterFiles
{
    Task ExportAsync(CharacterDto character, string path, CancellationToken cancellationToken = default);
    Task<CharacterDto> ReadAsync(string path, CancellationToken cancellationToken = default);
}
