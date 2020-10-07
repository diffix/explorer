namespace Explorer.Queries
{
    using System;
    using System.Text.Json;
    using Diffix;

    public class IdentityProjection : ColumnProjection
    {
        private readonly DValueType columnType;

        public IdentityProjection(string column, int index, DValueType columnType)
        : base(column, index)
        {
            this.columnType = columnType;
        }

        public override string Project() => $"\"{Column}\"";

        public override object? Invert(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return columnType switch
            {
                DValueType.Bool => value.GetBoolean(),
                DValueType.Integer => value.GetInt64(),
                DValueType.Real => value.GetDouble(),
                DValueType.Date => value.GetDateTime(),
                DValueType.Datetime => value.GetDateTime(),
                DValueType.Text => value.GetString(),
                DValueType.Timestamp => value.GetDateTime(),
                DValueType.Unknown => value.GetString(),
                _ => throw new InvalidOperationException($"Unknown column type <{columnType}>"),
            };
        }
    }
}