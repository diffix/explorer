namespace Diffix
{
    /// <summary>
    /// Represents the name of an SQL object, i.e. a table or a column.
    /// This is used for quoting SQL object names in statements.
    /// </summary>
    public class DSqlObjectName
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DSqlObjectName"/> class from a string object.
        /// </summary>
        /// <param name="name">The name of the SQL object as a string.</param>
        public DSqlObjectName(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the contained SQL object name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Converts the object to a quoted string that can be used in SQL statements.
        /// </summary>
        /// <returns>The quoted name.</returns>
        public override string ToString()
        {
            return "\"" + Name + "\"";
        }
    }
}
