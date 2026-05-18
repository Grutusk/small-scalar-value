using System;
using System.Collections.Generic;

namespace ScalarValues.Collections;

/// <summary>
/// Small factory helpers that replace the few direct <c>Enumerable</c> calls used in the project.
/// </summary>
internal static class Enumerable
{
    internal static IEnumerable<TSource> Empty<TSource>()
    {
        return Array.Empty<TSource>();
    }

    internal static IEnumerable<int> Range(int start, int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        var values = new int[count];
        for (var index = 0; index < count; index++)
            values[index] = start + index;

        return values;
    }
}
