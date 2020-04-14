namespace Diffix
{
    public interface DRowParser
    {
        /// <summary>
        /// Parses the result of a count() aggregate.
        /// </summary>
        /// <returns>The COUNT value as a <c>long</c>.</returns>
        long ParseCount();

        /// <summary>
        /// Parses the result of the grouping_id() function.
        /// </summary>
        /// <returns>The parsed grouping_id() result value.</returns>
        int ParseGroupingId();

        (int, DValue<T>) ParseGroupingSet<T>(int groupSize);

        /// <summary>
        /// Parses the result of the count_noise() aircloak function.
        /// </summary>
        /// <returns>A value of type <c>double</c>, or null.</returns>
        double? ParseCountNoise();

        /// <summary>
        /// Parses the result of a *_noise() aircloak function.
        /// </summary>
        /// <returns>A value of type <c>double</c>, or null.</returns>
        double? ParseNoise();

        /// <summary>
        /// Parses a suppressible, nullable column value using the default parser for the provided type
        /// parameter <c>T</c>.
        /// </summary>
        /// <typeparam name="T">Type of the parsed value.</typeparam>
        /// <returns>The parsed value wrapped in an <see cref="DValue{T}"/>.</returns>
        DValue<T> ParseValue<T>();
    }
}