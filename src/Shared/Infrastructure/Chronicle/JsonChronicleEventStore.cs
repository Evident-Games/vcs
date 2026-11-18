using System.Text.Json;
using Game.Application.Chronicle;
using Game.Infrastructure.Persistence;

namespace Game.Infrastructure.Chronicle;

/// <summary>Stores chronicle events beside the selected character database with atomic replacement.</summary>
public sealed class JsonChronicleEventStore(DatabaseAccess database) : IChronicleEventStore
{
    private string Path => database.Path + ".chronicle-events.json";
    public async Task<IReadOnlyList<ChronicleEvent>> LoadAsync()
    {
        if (!File.Exists(Path)) return [];
        await using var stream = File.OpenRead(Path);
        return await JsonSerializer.DeserializeAsync<ChronicleEvent[]>(stream)
            ?? throw new InvalidDataException("O registro de eventos está vazio ou inválido.");
    }

    public async Task SaveAsync(IReadOnlyList<ChronicleEvent> events)
    {
        var temporary = Path + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                await JsonSerializer.SerializeAsync(stream, events);
            File.Move(temporary, Path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
