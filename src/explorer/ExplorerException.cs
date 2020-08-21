namespace Explorer
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Diffix;

    [System.Serializable]
    public class ExplorerException : System.Exception
    {
        public ExplorerException()
        {
        }

        public ExplorerException(string message)
        : base(message)
        {
        }

        public ExplorerException(string message, System.Exception inner)
        : base(message, inner)
        {
        }

        protected ExplorerException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }

        public ExplorationContext? ExtraContext { get; private set; }

        public ExplorerException WithExtraContext(ExplorerContext? context)
        {
            if (!(context is null))
            {
                ExtraContext = new ExplorationContext
                {
                    DataSource = context.DataSource,
                    Table = context.Table,
                    Columns = ImmutableDictionary.CreateRange(
                        context.Columns
                            .Zip(context.ColumnInfos)
                            .Select(kv => new KeyValuePair<string, DColumnInfo>(kv.First, kv.Second))),
                };
                Data[ExplorationContext.DataKey] = ExtraContext;
            }
            return this;
        }

        public class ExplorationContext
        {
            public static string DataKey { get => typeof(ExplorationContext).FullName!; }

            public string? DataSource { get; set; }

            public string? Table { get; set; }

            public ImmutableDictionary<string, DColumnInfo>? Columns { get; set; }
        }
    }
}