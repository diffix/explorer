namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    public enum TextColumnTrimType
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
        IQuerySpec<TextColumnTrim.Result>
    {
        public TextColumnTrim(string tableName, string columnName, TextColumnTrimType trimType, string trimChars)
        {
            TableName = tableName;
            ColumnName = columnName;
            TrimPosition = trimType.ToString().ToUpperInvariant();
            TrimChars = trimChars;
        }

        public string QueryStatement => $@"
            select 
                trim({TrimPosition} '{TrimChars}' FROM {ColumnName}),
                count(*),
                count_noise(*)
            from {TableName}
            group by 1";

        private string TableName { get; }

        private string ColumnName { get; }

        private string TrimPosition { get; }

        private string TrimChars { get; }

        public Result FromJsonArray(ref Utf8JsonReader reader) => new Result(ref reader);

        public class Result : ICountAggregate, IDiffixValue
        {
            private readonly IDiffixValue<string> trimmedText;

            public Result(ref Utf8JsonReader reader)
            {
                trimmedText = reader.ParseAircloakResultValue<string>();
                Count = reader.ParseCount();
                CountNoise = reader.ParseCountNoise();
            }

            public string? TrimmedText => trimmedText.HasValue ? trimmedText.Value : null;

            public long Count { get; set; }

            public double? CountNoise { get; set; }

            public bool IsNull => trimmedText.IsNull;

            public bool IsSuppressed => trimmedText.IsSuppressed;
        }
    }
}