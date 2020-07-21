namespace Explorer.Common
{
    using Diffix;

    public class ColumnInfo
    {
        public ColumnInfo(DValueType columnType, bool userId, bool isolating)
        {
            Type = columnType;
            UserId = userId;
            Isolating = isolating;
        }

        public ColumnInfo(DValueType columnType, ColumnType type)
        {
            Type = columnType;
            UserId = type == ColumnType.UserId;
            Isolating = type != ColumnType.Regular;
        }

        public enum ColumnType
        {
            /// <summary>
            /// The column is an user_id column.
            /// </summary>
            UserId,

            /// <summary>
            /// The column is an isolating column.
            /// </summary>
            Isolating,

            /// <summary>
            /// The column is a regular column (i.e. not isolating and not user_id)
            /// </summary>
            Regular,
        }

        public DValueType Type { get; }

        public bool UserId { get; }

        public bool Isolating { get; }
    }
}
