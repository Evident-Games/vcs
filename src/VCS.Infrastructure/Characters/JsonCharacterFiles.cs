using System.Text.Json;
using System.Text.Json.Serialization;
using VCS.Application.Characters;
using VCS.Infrastructure.Persistence;
using VCS.Rules.V5.Validation;
using VCS.Sheet;

namespace VCS.Infrastructure.Characters;

public sealed class JsonCharacterFiles : ICharacterFiles
{
    private const long MaxBytes = 5 * 1024 * 1024;
    // 13: content ids carry their kind in English (older files are upgraded on read).
    private const int FormatVersion = 13;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter<TraitCategory>(allowIntegerValues: false) }
    };

    public async Task ExportAsync(CharacterDto character, string path, CancellationToken cancellationToken = default)
    {
        var issues = CharacterRules.Validate(character.Details, character.Traits);
        if (issues.Count != 0)
            throw new CharacterValidationException(issues);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, new CharacterFile(FormatVersion, character), Options, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public async Task<CharacterDto> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > MaxBytes)
            throw new InvalidDataException("O JSON excede o limite de 5 MB.");
        var file = await JsonSerializer.DeserializeAsync<CharacterFile>(stream, Options, cancellationToken);
        if (file is null || file.FormatVersion is < 1 or > FormatVersion || file.Character is not { } loaded ||
            loaded.Details is null || loaded.Traits is null || loaded.Traits.Any(x => x is null))
            throw new InvalidDataException("Formato de ficha invalido ou versao nao suportada.");
        var (details, traits) = new LegacyContentIds().Upgrade(loaded.Details, loaded.Traits);
        var character = loaded with { Details = details, Traits = traits };
        var issues = CharacterRules.Validate(character.Details, character.Traits);
        if (issues.Count != 0)
            throw new CharacterValidationException(issues);
        return character;
    }

    private sealed record CharacterFile(int FormatVersion, CharacterDto? Character);
}
