using System;
using System.Collections;
using System.Collections.Generic;

namespace ScalarValues.Collections;

/// <summary>
/// Stores an ordered view over a copied source sequence without relying on LINQ.
/// </summary>
internal sealed class OrderedSequence<TSource> : IEnumerable<TSource>
{
    private readonly List<TSource> _source;
    private readonly Comparison<TSource> _comparison;

    internal OrderedSequence(IEnumerable<TSource> source, Comparison<TSource> comparison)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(comparison);

        _source = source is List<TSource> list
            ? new List<TSource>(list)
            : new List<TSource>(source);
        _comparison = comparison;
    }

    internal OrderedSequence<TSource> ThenBy<TKey>(
        Func<TSource, TKey> keySelector,
        IComparer<TKey> comparer,
        bool descending)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        var resolvedComparer = comparer ?? Comparer<TKey>.Default;
        Comparison<TSource> additionalComparison = (left, right) =>
        {
            var comparison = resolvedComparer.Compare(keySelector(left), keySelector(right));
            return descending ? -comparison : comparison;
        };

        return new OrderedSequence<TSource>(_source, (left, right) =>
        {
            var primaryComparison = _comparison(left, right);
            return primaryComparison != 0
                ? primaryComparison
                : additionalComparison(left, right);
        });
    }

    internal List<TSource> ToSortedList()
    {
        var results = new List<TSource>(_source);
        results.Sort(_comparison);
        return results;
    }

    public IEnumerator<TSource> GetEnumerator()
    {
        return ToSortedList().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
