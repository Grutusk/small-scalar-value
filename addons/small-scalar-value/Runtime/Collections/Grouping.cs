using System.Collections;
using System.Collections.Generic;

namespace ScalarValues.Collections;

/// <summary>
/// Simple grouping container used by the project's non-LINQ collection helpers.
/// </summary>
internal sealed class Grouping<TKey, TElement> : IEnumerable<TElement>
{
    private readonly List<TElement> _items = [];

    internal Grouping(TKey key)
    {
        Key = key;
    }

    internal TKey Key { get; }

    internal void Add(TElement item)
    {
        _items.Add(item);
    }

    public IEnumerator<TElement> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
