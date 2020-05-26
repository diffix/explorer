namespace Explorer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Diffix;
    using Explorer.Common;

    public class QueryableTestScope : IDisposable
    {
        public TestScope Inner { get; }

        public QueryableTestScope(TestScope testScope)
        {
            Inner = testScope;
        }

        public async Task<IEnumerable<TRow>> QueryRows<TRow>(DQuery<TRow> query)
        {
            var queryResult = await Inner.Scope.GetInstance<DConnection>().Exec(query);

            return queryResult.Rows;
        }

        public void CancelQuery() => Inner.Scope.GetInstance<CancellationTokenSource>().Cancel();

        public async Task CancelQuery(int millisecondDelay)
        {
            await Task.Delay(millisecondDelay);
            CancelQuery();
        }

        public ComponentTestScope WithContext(
            string tableName,
            string columnName,
            DValueType columnType = DValueType.Unknown)
        {
            Inner.Scope.Inject<ExplorerContext>(
                new RawExplorerContext
                {
                    Table = tableName,
                    Column = columnName,
                    ColumnType = columnType,
                });
            return new ComponentTestScope(Inner);
        }

        public void Dispose()
        {
            Inner.Dispose();
        }
    }
}
