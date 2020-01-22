#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1815 // Struct type should override Equals
#pragma warning disable CA1034 // Do not nest types

namespace Aircloak.JsonApi.ResponseTypes
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the JSON response from a request to /api/data_sources.
    /// </summary>
    public struct DataSource
    {
        public string Name { get; set; }

        public string Description { get; set; }

        [JsonIgnore]
        public IDictionary<string, Table> TableDict
        {
            get
            {
                return Tables.ToDictionary(table => table.Id);
            }
        }

        public IEnumerable<Table> Tables { get; set; }

        public struct Table
        {
            public string Id { get; set; }

            public IEnumerable<Column> Columns { get; set; }

            [JsonIgnore]
            public IDictionary<string, Column> ColumnDict
            {
                get
                {
                    return Columns.ToDictionary(column => column.Name);
                }
            }

            public struct Column
            {
                public string Name { get; set; }

                public AircloakType Type { get; set; }
            }
        }
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore CA1815 // Struct type should override Equals
#pragma warning restore CA1034 // Do not nest types