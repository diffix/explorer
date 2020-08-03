namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    internal class TextColumnPrefix :
        DQuery,
        DResultParser<ValueWithCount<string>>
    {
        private readonly int length;

        public TextColumnPrefix(int length)
        {
            this.length = length;
        }

        protected override string GetQueryStatement(string table, string column)
        {
            return $@"
                select
                    left({column}, {length}),
                    count(*),
                    count_noise(*)
                from {table}
                group by 1
                having length(left({column}, {length})) = {length}";
        }

        public ValueWithCount<string> ParseRow(ref Utf8JsonReader reader) =>
            new ValueWithCount<string>(ref reader);
    }
}