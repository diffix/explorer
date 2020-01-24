namespace Aircloak.JsonApi.ResponseTypes
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents the JSON response from a request to /api/data_sources and provides a getter
    /// to return the list of value as a Dict indexed by the datasource's name.
    /// </summary>
    public class DataSourceCollection : List<DataSource>
    {
        /// <summary>
        /// Gets the datasources as a Dict indexed by datasource name.
        /// </summary>
        public IDictionary<string, DataSource> AsDict
        {
            get
            {
                return this.ToDictionary(dataSource => dataSource.Name);
            }
        }
    }