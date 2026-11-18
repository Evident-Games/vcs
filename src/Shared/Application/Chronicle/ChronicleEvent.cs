namespace Game.Application.Chronicle;

/// <summary>An event adjudicated by the Storyteller, independent of character sheets.</summary>
public sealed record ChronicleEvent(Guid Id, DateTime Date, string Chronicle, string Kind, string Title,
    Guid? CharacterId, string CharacterName, string Notes, string Outcome, string Source, ChronicleEventState? State = null);

public interface IChronicleEventStore
{
    Task<IReadOnlyList<ChronicleEvent>> LoadAsync();
    Task SaveAsync(IReadOnlyList<ChronicleEvent> events);
}

/// <summary>Structured scene state; older events without this field remain readable.</summary>
public sealed record ChronicleEventState(
    string Counterparty = "", int BondLevel = 0, DateTime? LastDrink = null,
    string Effect = "", string Discipline = "", string Expiration = "", bool Active = true,
    int Round = 1, int FirstPool = 5, int SecondPool = 5, int FirstHunger = 0, int SecondHunger = 0,
    int FirstWeapon = 0, int SecondWeapon = 0, string Formula = "", string Ingredients = "",
    string Resonance = "", int Alchemy = 1, int FormulaLevel = 1, int AlchemyAttribute = 1,
    int IngredientModifier = 0, string Method = "AthanorCorporis", bool Improvised = false);
