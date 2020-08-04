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
        private readonly TextColumnTrimType trimType;
        private readonly string trimChars;

        public TextColumnTrim(TextColumnTrimType trimType, string trimChars)
        {
            this.trimType = trimType;
            this.trimChars = trimChars;
        }

        public override ValueWithCount<string> ParseRow(ref Utf8JsonReader reader) =>
            new ValueWithCount<string>(ref reader);

        protected override string GetQueryStatement(string table, string column)
        {
            var trimPosition = trimType.ToString().ToUpperInvariant();

            return $@"
                select
                    trim({trimPosition} '{trimChars}' FROM {column}),
                    count(*),
                    count_noise(*)
                from {table}
                group by 1";
        }
    }
}