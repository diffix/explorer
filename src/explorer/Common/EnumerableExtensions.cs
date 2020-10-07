namespace Explorer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Utility extensions for IEnumerable sequences.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Enumerates all possible pairwise combinations of a given sequence.
        /// For example, given a sequece <c>{'A', 'B', 'C'}</c>, it returns
        /// <c>{ ('A', 'B'), ('B', 'C'), ('A', 'C') }</c>.
        /// </summary>
        /// <param name="values">The input sequence.</param>
        /// <typeparam name="T">The type of values contained in the sequence.</typeparam>
        /// <returns>
        /// A sequence of tuples conaining distinct pairs.
        /// </returns>
        public static IEnumerable<(T, T)> EnumeratePairs<T>(this IEnumerable<T> values)
        {
            ThrowArgumentExceptionIfNull(values);

            foreach (var i in Enumerable.Range(1, values.Count() - 1))
            {
                foreach (var pair in values.Zip(values.Skip(i)))
                {
                    yield return pair;
                }
            }
        }

        public static IEnumerable<IEnumerable<T>> EnumeratePermutations<T>(this IEnumerable<T> values, int size)
        {
            ThrowArgumentExceptionIfNull(values);

            IEnumerable<T[]> acc = new[] { Array.Empty<T>() };
            foreach (var i in Enumerable.Range(0, size))
            {
                acc = acc.EnumerateCombinations(values);
                foreach (var v in acc)
                {
                    yield return v;
                }
            }
        }

        public static IEnumerable<(T1, T2, T3)> Zip2<T1, T2, T3>(
            this IEnumerable<T1> values1,
            IEnumerable<T2> values2,
            IEnumerable<T3> values3)
        {
            ThrowArgumentExceptionIfNull(values1);
            ThrowArgumentExceptionIfNull(values2);
            ThrowArgumentExceptionIfNull(values3);

            foreach (var (v1, (v2, v3)) in values1.Zip(values2.Zip(values3)))
            {
                yield return (v1, v2, v3);
            }
        }

        private static IEnumerable<T[]> EnumerateCombinations<T>(this IEnumerable<T[]> lists, IEnumerable<T> values)
        {
            ThrowArgumentExceptionIfNull(lists);
            ThrowArgumentExceptionIfNull(values);

            var i = 0;
            foreach (var l in lists)
            {
                foreach (var v in values.Skip(l.Length + i))
                {
                    yield return l.Append(v).ToArray();
                }
                i++;
            }
        }

        private static void ThrowArgumentExceptionIfNull<T>(T arg)
        {
            if (arg is null)
            {
                throw new ArgumentNullException($"{typeof(T)} parameter cannot be null.");
            }
        }
    }
}