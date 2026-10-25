namespace VCS.Domain.Characters;

public sealed record NpcAttack(string Name = "", string DicePool = "", string DamageBonus = "",
    string DamageType = "", string ActiveDisciplines = "", string Notes = "");

// Value-equality list so CharacterDetails record comparisons ignore the collection instance.

public sealed class NpcAttackList(IEnumerable<NpcAttack> attacks) : IReadOnlyList<NpcAttack>, IEquatable<NpcAttackList>
{
    private readonly NpcAttack[] _items = [.. attacks];
    public static NpcAttackList Empty { get; } = new([]);
    public NpcAttack this[int index] => _items[index];
    public int Count => _items.Length;
    public IEnumerator<NpcAttack> GetEnumerator() => ((IEnumerable<NpcAttack>)_items).GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
    public bool Equals(NpcAttackList? other) => other is not null && _items.SequenceEqual(other._items);
    public override bool Equals(object? obj) => Equals(obj as NpcAttackList);
    public override int GetHashCode() => _items.Aggregate(_items.Length, HashCode.Combine);
}
