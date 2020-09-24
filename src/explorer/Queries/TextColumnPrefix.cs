namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.Common.Utils;

    internal class TextColumnPrefix :
        DQuery<ValueWithCountRow<string>>
    {
        private readonly int length;

        public TextColumnPrefix(int length)
        {
            this.length = length;
        }

        public override ValueWithCountRow<string> ParseRow(ref Utf8JsonReader reader) =>
            new ValueWithCountRow<string>(ref reader);

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
    }
}