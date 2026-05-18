using System;
using System.Collections;
using System.Collections.Generic;

namespace ScalarValues.Collections;

/// <summary>
/// Project-owned collection helpers that replace LINQ in Godot code.
/// </summary>
internal static class CollectionExtensions
{
    internal static bool Any<TSource>(this IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var enumerator = source.GetEnumerator();
        return enumerator.MoveNext();
    }

    internal static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (var item in source)
        {
            if (predicate(item))
                return true;
        }

        return false;
    }

    internal static bool All<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (var item in source)
        {
            if (!predicate(item))
                return false;
        }

        return true;
    }

    internal static int Count<TSource>(this IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is ICollection<TSource> collection)
            return collection.Count;

        if (source is ICollection nonGenericCollection)
            return nonGenericCollection.Count;

        var count = 0;
        foreach (var _ in source)
            count++;

        return count;
    }

    internal static int Count<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        var count = 0;
        foreach (var item in source)
        {
            if (predicate(item))
                count++;
        }

        return count;
    }

    internal static TSource First<TSource>(this IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var enumerator = source.GetEnumerator();
        if (enumerator.MoveNext())
            return enumerator.Current;

        throw new InvalidOperationException("Sequence contains no elements.");
    }

    internal static TSource First<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (var item in source)
        {
            if (predicate(item))
                return item;
        }

        throw new InvalidOperationException("Sequence contains no matching element.");
    }

    internal static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var enumerator = source.GetEnumerator();
        return enumerator.MoveNext() ? enumerator.Current : default;
    }

    internal static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (var item in source)
        {
            if (predicate(item))
                return item;
        }

        return default;
    }

    internal static TSource Single<TSource>(this IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            throw new InvalidOperationException("Sequence contains no elements.");

        var result = enumerator.Current;
        if (enumerator.MoveNext())
            throw new InvalidOperationException("Sequence contains more than one element.");

        return result;
    }

    internal static TSource Single<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        var hasMatch = false;
        var result = default(TSource);
        foreach (var item in source)
        {
            if (!predicate(item))
                continue;

            if (hasMatch)
                throw new InvalidOperationException("Sequence contains more than one matching element.");

            hasMatch = true;
            result = item;
        }

        if (!hasMatch)
            throw new InvalidOperationException("Sequence contains no matching element.");

        return result;
    }

    internal static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            return default;

        var result = enumerator.Current;
        if (enumerator.MoveNext())
            throw new InvalidOperationException("Sequence contains more than one element.");

        return result;
    }

    internal static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        var hasMatch = false;
        var result = default(TSource);
        foreach (var item in source)
        {
            if (!predicate(item))
                continue;

            if (hasMatch)
                throw new InvalidOperationException("Sequence contains more than one matching element.");

            hasMatch = true;
            result = item;
        }

        return result;
    }

    internal static TSource Last<TSource>(this IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var hasValue = false;
        var last = default(TSource);
        foreach (var item in source)
        {
            hasValue = true;
            last = item;
        }

        if (hasValue)
            return last;

        throw new InvalidOperationException("Sequence contains no elements.");
    }

    internal static TSource Last<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        var hasValue = false;
        var last = default(TSource);
        foreach (var item in source)
        {
            if (!predicate(item))
                continue;

            hasValue = true;
            last = item;
        }

        if (hasValue)
            return last;

        throw new InvalidOperationException("Sequence contains no matching element.");
    }

    internal static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var hasValue = false;
        var last = default(TSource);
        foreach (var item in source)
        {
            hasValue = true;
            last = item;
        }

        return hasValue ? last : default;
    }

    internal static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        var hasValue = false;
        var last = default(TSource);
        foreach (var item in source)
        {
            if (!predicate(item))
                continue;

            hasValue = true;
            last = item;
        }

        return hasValue ? last : default;
    }

    internal static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value)
    {
        return Contains(source, value, null);
    }

    internal static bool Contains<TSource>(
        this IEnumerable<TSource> source,
        TSource value,
        IEqualityComparer<TSource> comparer)
    {
        ArgumentNullException.ThrowIfNull(source);

        var resolvedComparer = comparer ?? EqualityComparer<TSource>.Default;
        foreach (var item in source)
        {
            if (resolvedComparer.Equals(item, value))
                return true;
        }

        return false;
    }

    internal static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        var results = source is ICollection<TSource> collection
            ? new List<TSource>(collection.Count)
            : new List<TSource>();
        foreach (var item in source)
        {
            if (predicate(item))
                results.Add(item);
        }

        return results;
    }

    internal static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var results = source is ICollection<TSource> collection
            ? new List<TResult>(collection.Count)
            : new List<TResult>();
        foreach (var item in source)
            results.Add(selector(item));

        return results;
    }

    internal static IEnumerable<TResult> SelectMany<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, IEnumerable<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var results = new List<TResult>();
        foreach (var item in source)
        {
            var selected = selector(item);
            if (selected == null)
                continue;

            foreach (var innerItem in selected)
                results.Add(innerItem);
        }

        return results;
    }

    internal static IEnumerable<TResult> OfType<TResult>(this IEnumerable source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var results = new List<TResult>();
        foreach (var item in source)
        {
            if (item is TResult typedItem)
                results.Add(typedItem);
        }

        return results;
    }

    internal static IEnumerable<TSource> Skip<TSource>(this IEnumerable<TSource> source, int count)
    {
        ArgumentNullException.ThrowIfNull(source);

        var results = new List<TSource>();
        var skipCount = Math.Max(0, count);
        var index = 0;
        foreach (var item in source)
        {
            if (index++ < skipCount)
                continue;

            results.Add(item);
        }

        return results;
    }

    internal static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, int count)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (count <= 0)
            return Array.Empty<TSource>();

        var results = new List<TSource>(count);
        var remaining = count;
        foreach (var item in source)
        {
            if (remaining-- <= 0)
                break;

            results.Add(item);
        }

        return results;
    }

    internal static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source)
    {
        return Distinct(source, null);
    }

    internal static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
    {
        ArgumentNullException.ThrowIfNull(source);

        var seen = new HashSet<TSource>(comparer);
        var results = new List<TSource>();
        foreach (var item in source)
        {
            if (seen.Add(item))
                results.Add(item);
        }

        return results;
    }

    internal static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
    {
        return Intersect(first, second, null);
    }

    internal static IEnumerable<TSource> Intersect<TSource>(
        this IEnumerable<TSource> first,
        IEnumerable<TSource> second,
        IEqualityComparer<TSource> comparer)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        var remaining = new HashSet<TSource>(second, comparer);
        var results = new List<TSource>();
        foreach (var item in first)
        {
            if (remaining.Remove(item))
                results.Add(item);
        }

        return results;
    }

    internal static IEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        return GroupBy(source, keySelector, null);
    }

    internal static IEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey> comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        var groups = new List<Grouping<TKey, TSource>>();
        var groupsByKey = new Dictionary<TKey, Grouping<TKey, TSource>>(comparer);
        Grouping<TKey, TSource> nullGroup = null;
        var hasNullGroup = false;

        foreach (var item in source)
        {
            var key = keySelector(item);
            if (key is null)
            {
                if (!hasNullGroup)
                {
                    nullGroup = new Grouping<TKey, TSource>(key);
                    groups.Add(nullGroup);
                    hasNullGroup = true;
                }

                nullGroup.Add(item);
                continue;
            }

            if (!groupsByKey.TryGetValue(key, out var group))
            {
                group = new Grouping<TKey, TSource>(key);
                groupsByKey.Add(key, group);
                groups.Add(group);
            }

            group.Add(item);
        }

        return groups;
    }

    internal static OrderedSequence<TSource> OrderBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        return OrderBy(source, keySelector, null);
    }

    internal static OrderedSequence<TSource> OrderBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IComparer<TKey> comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        return new OrderedSequence<TSource>(source, CreateComparison(keySelector, comparer, descending: false));
    }

    internal static OrderedSequence<TSource> OrderByDescending<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        return OrderByDescending(source, keySelector, null);
    }

    internal static OrderedSequence<TSource> OrderByDescending<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IComparer<TKey> comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        return new OrderedSequence<TSource>(source, CreateComparison(keySelector, comparer, descending: true));
    }

    internal static OrderedSequence<TSource> ThenBy<TSource, TKey>(
        this OrderedSequence<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        return ThenBy(source, keySelector, null);
    }

    internal static OrderedSequence<TSource> ThenBy<TSource, TKey>(
        this OrderedSequence<TSource> source,
        Func<TSource, TKey> keySelector,
        IComparer<TKey> comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        return source.ThenBy(keySelector, comparer, descending: false);
    }

    internal static OrderedSequence<TSource> ThenByDescending<TSource, TKey>(
        this OrderedSequence<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        return ThenByDescending(source, keySelector, null);
    }

    internal static OrderedSequence<TSource> ThenByDescending<TSource, TKey>(
        this OrderedSequence<TSource> source,
        Func<TSource, TKey> keySelector,
        IComparer<TKey> comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        return source.ThenBy(keySelector, comparer, descending: true);
    }

    internal static List<TSource> ToList<TSource>(this OrderedSequence<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.ToSortedList();
    }

    internal static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is List<TSource> list)
            return new List<TSource>(list);

        if (source is ICollection<TSource> collection)
        {
            var copy = new List<TSource>(collection.Count);
            foreach (var item in source)
                copy.Add(item);

            return copy;
        }

        return new List<TSource>(source);
    }

    internal static TSource[] ToArray<TSource>(this OrderedSequence<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.ToSortedList().ToArray();
    }

    internal static TSource[] ToArray<TSource>(this IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is TSource[] array)
        {
            var copy = new TSource[array.Length];
            Array.Copy(array, copy, array.Length);
            return copy;
        }

        return ToList(source).ToArray();
    }

    internal static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        var results = ToList(first);
        foreach (var item in second)
            results.Add(item);

        return results;
    }

    internal static IEnumerable<TSource> Append<TSource>(this IEnumerable<TSource> source, TSource item)
    {
        ArgumentNullException.ThrowIfNull(source);

        var results = ToList(source);
        results.Add(item);
        return results;
    }

    internal static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
    {
        return Union(first, second, null);
    }

    internal static IEnumerable<TSource> Union<TSource>(
        this IEnumerable<TSource> first,
        IEnumerable<TSource> second,
        IEqualityComparer<TSource> comparer)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        var seen = new HashSet<TSource>(comparer);
        var results = new List<TSource>();
        foreach (var item in first)
        {
            if (seen.Add(item))
                results.Add(item);
        }

        foreach (var item in second)
        {
            if (seen.Add(item))
                results.Add(item);
        }

        return results;
    }

    internal static IEnumerable<TSource> DefaultIfEmpty<TSource>(this IEnumerable<TSource> source)
    {
        return DefaultIfEmpty(source, default);
    }

    internal static IEnumerable<TSource> DefaultIfEmpty<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
    {
        ArgumentNullException.ThrowIfNull(source);

        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            return [defaultValue];

        var results = new List<TSource> { enumerator.Current };
        while (enumerator.MoveNext())
            results.Add(enumerator.Current);

        return results;
    }

    internal static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
    {
        return ToHashSet(source, null);
    }

    internal static HashSet<TSource> ToHashSet<TSource>(
        this IEnumerable<TSource> source,
        IEqualityComparer<TSource> comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new HashSet<TSource>(source, comparer);
    }

    internal static int Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var sum = 0;
        foreach (var item in source)
            sum += selector(item);

        return sum;
    }

    internal static double Average(this IEnumerable<double> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var sum = 0d;
        var count = 0;
        foreach (var item in source)
        {
            sum += item;
            count++;
        }

        if (count == 0)
            throw new InvalidOperationException("Sequence contains no elements.");

        return sum / count;
    }

    internal static float Average(this IEnumerable<float> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var sum = 0f;
        var count = 0;
        foreach (var item in source)
        {
            sum += item;
            count++;
        }

        if (count == 0)
            throw new InvalidOperationException("Sequence contains no elements.");

        return sum / count;
    }

    internal static int Max(this IEnumerable<int> source)
    {
        return Max<int, int>(source, static item => item);
    }

    internal static double Max(this IEnumerable<double> source)
    {
        return Max<double, double>(source, static item => item);
    }

    internal static float Max(this IEnumerable<float> source)
    {
        return Max<float, float>(source, static item => item);
    }

    internal static TResult Max<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var comparer = Comparer<TResult>.Default;
        var hasValue = false;
        var best = default(TResult);
        foreach (var item in source)
        {
            var candidate = selector(item);
            if (!hasValue || comparer.Compare(candidate, best) > 0)
            {
                best = candidate;
                hasValue = true;
            }
        }

        if (hasValue)
            return best;

        throw new InvalidOperationException("Sequence contains no elements.");
    }

    internal static TSource Max<TSource>(this IEnumerable<TSource> source)
    {
        return Max<TSource, TSource>(source, static item => item);
    }

    internal static int Min(this IEnumerable<int> source)
    {
        return Min<int, int>(source, static item => item);
    }

    internal static double Min(this IEnumerable<double> source)
    {
        return Min<double, double>(source, static item => item);
    }

    internal static float Min(this IEnumerable<float> source)
    {
        return Min<float, float>(source, static item => item);
    }

    internal static TResult Min<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var comparer = Comparer<TResult>.Default;
        var hasValue = false;
        var best = default(TResult);
        foreach (var item in source)
        {
            var candidate = selector(item);
            if (!hasValue || comparer.Compare(candidate, best) < 0)
            {
                best = candidate;
                hasValue = true;
            }
        }

        if (hasValue)
            return best;

        throw new InvalidOperationException("Sequence contains no elements.");
    }

    internal static TSource Min<TSource>(this IEnumerable<TSource> source)
    {
        return Min<TSource, TSource>(source, static item => item);
    }

    internal static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        return ToDictionary(source, keySelector, null);
    }

    internal static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey> comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        var dictionary = new Dictionary<TKey, TSource>(comparer);
        foreach (var item in source)
            dictionary.Add(keySelector(item), item);

        return dictionary;
    }

    internal static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector)
    {
        return ToDictionary(source, keySelector, elementSelector, null);
    }

    internal static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector,
        IEqualityComparer<TKey> comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(elementSelector);

        var dictionary = new Dictionary<TKey, TElement>(comparer);
        foreach (var item in source)
            dictionary.Add(keySelector(item), elementSelector(item));

        return dictionary;
    }

    private static Comparison<TSource> CreateComparison<TSource, TKey>(
        Func<TSource, TKey> keySelector,
        IComparer<TKey> comparer,
        bool descending)
    {
        var resolvedComparer = comparer ?? Comparer<TKey>.Default;
        return (left, right) =>
        {
            var comparison = resolvedComparer.Compare(keySelector(left), keySelector(right));
            return descending ? -comparison : comparison;
        };
    }
}
