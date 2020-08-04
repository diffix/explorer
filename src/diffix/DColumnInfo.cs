namespace Diffix
{
    /// <summary>
    /// Stores Diffix metadata information about a database column.
    /// </summary>
    public class DColumnInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DColumnInfo"/> class.
        /// </summary>
        /// <param name="columnType">Specifies the column data type.</param>
        /// <param name="userId">Specifies whether the column contains a "user_id" information.</param>
        /// <param name="isolating">Specifies whether the column is "isolating" or not.</param>
        public DColumnInfo(DValueType columnType, bool userId, bool isolating)
        {
            Type = columnType;
            UserId = userId;
            Isolating = isolating;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DColumnInfo"/> class.
        /// </summary>
        /// <param name="columnType">Specifies the column data type.</param>
        /// <param name="type">Specifies additional information used by Diffix for the column: user_id/isolating/regular.</param>
        public DColumnInfo(DValueType columnType, ColumnType type)
        {
            Type = columnType;
            UserId = type == ColumnType.UserId;
            Isolating = type != ColumnType.Regular;
        }

        /// <summary>
        /// Describes additional information used by Diffix for a database column.
        /// </summary>
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

        /// <summary>
        /// Gets the column data type.
        /// </summary>
        public DValueType Type { get; }

        /// <summary>
        /// Gets a value indicating whether the column contains a "user_id" information.
        /// </summary>
        public bool UserId { get; }

        /// <summary>
        /// Gets a value indicating whether the column is "isolating" or not.
        /// </summary>
        public bool Isolating { get; }
    }
}
