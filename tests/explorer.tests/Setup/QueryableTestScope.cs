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
        private bool disposedValue;

        public QueryableTestScope(TestScope testScope)
        {
            Inner = testScope;
        }

        public TestScope Inner { get; }

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
            string dataSource,
            string tableName,
            string columnName,
            DValueType columnType = DValueType.Unknown)
        {
            Inner.Scope.Inject<ExplorerContext>(
                new ExplorerTestContext
                {
                    DataSource = dataSource,
                    Table = new DSqlObjectName(tableName),
                    Column = new DSqlObjectName(columnName),
                    ColumnType = columnType,
                });
            return new ComponentTestScope(Inner);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Inner.Dispose();
                }

                disposedValue = true;
            }
        }
    }
}
