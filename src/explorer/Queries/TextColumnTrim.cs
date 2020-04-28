namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    internal enum TextColumnTrimType
    {
        /// <summary>
        /// Trim chars from the beginning.
        /// </summary>
        Leading,

        /// <summary>
        /// Trim chars at the end.
        /// </summary>
        Trailing,

        /// <summary>
        /// Trim chars at both ends.
        /// </summary>
        Both,
    }

    internal class TextColumnTrim :
        DQuery<ValueWithCount<string>>
    {
        public TextColumnTrim(string tableName, string columnName, TextColumnTrimType trimType, string trimChars)
        {
            var trimPosition = trimType.ToString().ToUpperInvariant();

            QueryStatement = $@"
                select 
                    trim({trimPosition} '{trimChars}' FROM {columnName}),
                    count(*),
                    count_noise(*)
                from {tableName}
                group by 1";
        }

        public string QueryStatement { get; }

        public ValueWithCount<string> ParseRow(ref Utf8JsonReader reader) =>
            new ValueWithCount<string>(ref reader);
    }
}