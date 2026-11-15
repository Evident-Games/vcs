namespace VCS.Application.Chronicle;

/// <summary>An event adjudicated by the Storyteller, independent of character sheets.</summary>
public sealed record ChronicleEvent(Guid Id, DateTime Date, string Chronicle, string Kind, string Title,
    Guid? CharacterId, string CharacterName, string Notes, string Outcome, string Source);

public interface IChronicleEventStore
{
    Task<IReadOnlyList<ChronicleEvent>> LoadAsync();
    Task SaveAsync(IReadOnlyList<ChronicleEvent> events);
}
