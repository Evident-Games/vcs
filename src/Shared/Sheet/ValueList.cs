namespace VCS.Sheet;

/// <summary>Read-only list with value equality, so CharacterDetails record comparisons ignore the collection instance.</summary>
public sealed class ValueList<T>(IEnumerable<T> items) : IReadOnlyList<T>, IEquatable<ValueList<T>>
{
    private readonly T[] _items = [.. items];
    public static ValueList<T> Empty { get; } = new([]);
    public T this[int index] => _items[index];
    public int Count => _items.Length;
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
    public bool Equals(ValueList<T>? other) => other is not null && _items.SequenceEqual(other._items);
    public override bool Equals(object? obj) => Equals(obj as ValueList<T>);
    public override int GetHashCode() => _items.Aggregate(_items.Length, (hash, item) => HashCode.Combine(hash, item));
}
