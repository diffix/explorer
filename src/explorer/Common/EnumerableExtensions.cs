namespace Explorer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
        /// Note that ('A', 'B') and ('B', 'A') are not considered distinct.
        /// </summary>
        /// <param name="values">The input sequence.</param>
        /// <typeparam name="T">The type of values contained in the sequence.</typeparam>
        /// <returns>
        /// A sequence of tuples containing distinct pairs.
        /// </returns>
        public static IEnumerable<(T, T)> EnumeratePairs<T>(this IEnumerable<T> values)
        {
            ThrowArgumentExceptionIfNull(values);

            if (values.Count() < 2)
            {
                yield break;
            }

            foreach (var i in Enumerable.Range(1, values.Count() - 1))
            {
                foreach (var pair in values.Zip(values.Skip(i)))
                {
                    yield return pair;
                }
            }
        }

        /// <summary>
        /// List groups of values up to a given maximum group size.
        /// </summary>
        /// <param name="values">The list of values.</param>
        /// <param name="size">Maximum size of subgroups.</param>
        /// <typeparam name="T">The type of the contained values.</typeparam>
        /// <returns>An Enumeration of subgroups, each of which is also Enumerable.</returns>
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

        /// <summary>
        /// Like <see cref="Zip" /> but with a total of three <c>IEnumerable</c>s. The resultant IEnumerable will have
        /// as many values as the shortest input IEnumerable.
        /// </summary>
        /// <param name="values1">The first IEnumerable.</param>
        /// <param name="values2">The second IEnumerable.</param>
        /// <param name="values3">The third IEnumerable.</param>
        /// <typeparam name="T1">The type of values in the first IEnumerable.</typeparam>
        /// <typeparam name="T2">The type of values in the second IEnumerable.</typeparam>
        /// <typeparam name="T3">The type of values in the third IEnumerable.</typeparam>
        /// <returns>An IEnumerable of 3-tuples of type (T1, T2, T3).</returns>
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

        /// <summary>
        /// Flattens an IEnumerable of IEnumerable{T} into a single IEnumerable{T}.
        /// </summary>
        /// <param name="values">The source IEnumerable{IEnumerable{T}}.</param>
        /// <typeparam name="T">The type of the contained values.</typeparam>
        /// <returns>A flattened list of values.</returns>
        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> values) => values.SelectMany(_ => _);

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