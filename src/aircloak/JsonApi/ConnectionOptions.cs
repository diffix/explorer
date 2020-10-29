namespace Aircloak.JsonApi
{
    /// <summary>
    /// Aircloak Api connection options.
    /// </summary>
    public class ConnectionOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of concurrent queries that will be run *per connection instance*.
        /// </summary>
        /// <value>Default value is 10.</value>
        public int MaxConcurrentQueries { get; set; } = 10;

        /// <summary>
        /// Gets or sets the time in milliseconds determining how long to wait when polling for query results. Note
        /// this is the maximum - the initial polling interval is short to optimise for quick responses. The polling
        /// interval then gradually increases to this maximum value.
        /// </summary>
        /// <value>Default is 2000.</value>
        public int PollingInterval { get; set; } = 2000;
    }
}
