namespace Explorer.Components
{
    using Diffix;
    using Explorer.Common;

    using Lamar;
    using Microsoft.Extensions.DependencyInjection;

    public class ComponentRegistry : ServiceRegistry
    {
        public ComponentRegistry()
        {
            // Scan for Components
            Scan(_ =>
            {
                _.Assembly("explorer");
                _.IncludeNamespace("Explorer.Components");
                _.AddAllTypesOf<PublisherComponent>(ServiceLifetime.Scoped);
                _.ConnectImplementationsToTypesClosing(typeof(ResultProvider<>), ServiceLifetime.Scoped);
                _.ConnectImplementationsToTypesClosing(typeof(ExplorerComponent<>), ServiceLifetime.Scoped);
            });

            // The following are not picked up by the scan for some reason, maybe because they close
            // primitive types (?)
            this.AddScoped<SimpleStats<double>>();
            this.AddScoped<SimpleStats<long>>();

            // Services to be injected at runtime
            Injectable<ExplorerContext>();
            Injectable<DConnection>();
        }
    }
}