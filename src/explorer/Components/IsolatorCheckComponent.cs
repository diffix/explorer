namespace Explorer.Components
{
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;

    public class IsolatorCheckComponent : ExplorerComponent<IsolatorCheckComponent.Result>
    {
        private readonly DConnection conn;
        private readonly ExplorerContext ctx;

        public IsolatorCheckComponent(DConnection conn, ExplorerContext ctx)
        {
            this.ctx = ctx;
            this.conn = conn;
        }

        protected override async Task<Result> Explore()
        {
            var isolators = await conn.Exec(new IsolatorQuery(ctx.Table));
            var isIsolatorColumn = isolators.Rows.First(r => r.Item1 == ctx.Column).Item2;

            return new Result(ctx.Column, isIsolatorColumn);
        }

        public class Result
        {
            public Result(string columnName, bool isIsolator)
            {
                ColumnName = columnName;
                IsIsolatorColumn = isIsolator;
            }

            public string ColumnName { get; }

            public bool IsIsolatorColumn { get; }
        }

        private class IsolatorQuery : DQuery<(string, bool)>
        {
            public IsolatorQuery(string table)
            {
                QueryStatement = $"show columns from {table}";
            }

            public string QueryStatement { get; }

            public (string, bool) ParseRow(ref Utf8JsonReader reader)
            {
                reader.Read();
                var name = reader.GetString();

                reader.Read(); // ignore data type

                reader.Read();
                var isolator = reader.GetString() == "true";

                reader.Read(); // ignore key type
                reader.Read(); // ignore comment

                return (name, isolator);
            }
        }
    }
}