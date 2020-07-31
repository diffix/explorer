namespace Explorer.Components
{
    using System.Linq;
    using BaselineTypeDiscovery;
    using Diffix;
    using Explorer.Common;

    using Lamar;
    using Lamar.Scanning.Conventions;
    using LamarCodeGeneration;
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
                _.ConnectImplementationsToTypesClosing(typeof(ResultProvider<>), ServiceLifetime.Scoped);

                // Register the custom convention
                _.Convention<PublisherScanner>();
            });

            // Services to be injected at runtime
            Injectable<ExplorerContext>();
            Injectable<DConnection>();

            // The following are not picked up by the scan for some reason, maybe because they close
            // primitive types (?)
            // RemoveAll(sd => sd.ImplementationType == typeof(SimpleStats<>));
            For<ResultProvider<SimpleStats<double>.Result>>().Use<SimpleStats<double>>().Scoped();
            For<ResultProvider<SimpleStats<long>.Result>>().Use<SimpleStats<long>>().Scoped();
            For<ResultProvider<SimpleStats<int>.Result>>().Use<SimpleStats<int>>().Scoped();
            For<ResultProvider<SimpleStats<decimal>.Result>>().Use<SimpleStats<decimal>>().Scoped();
        }

        /// <summary>
        /// This custom convention registers PublisherComponents. If the PublisherComponent is already registered as
        /// a ResultProvider, it forwards the invocation to the ResultProvider registration. This prevents duplicate
        /// components from being instantiated.
        /// </summary>
        private class PublisherScanner : IRegistrationConvention
        {
            public void ScanTypes(TypeSet types, ServiceRegistry services)
            {
                var publisherComponentTypes = types
                    .FindTypes(TypeClassification.Concretes | TypeClassification.Closed)
                    .Where(t => t.GetInterfaces().Contains(typeof(PublisherComponent)));

                // Register publisher components
                foreach (var type in publisherComponentTypes)
                {
                    var resultProviderInterface = type.GetInterfaces().SingleOrDefault(IsResultProviderInterface);

                    if (resultProviderInterface == default)
                    {
                        // Not yet registered
                        services.AddScoped(typeof(PublisherComponent), type);
                    }
                    else
                    {
                        // Already registered as ResultProvider, forward the invocation
                        services
                            .For<PublisherComponent>()
                            .Use(scope => (PublisherComponent)scope.GetService(resultProviderInterface))
                            .Named(type.NameInCode());
                    }
                }
            }

            private bool IsResultProviderInterface(System.Type i)
                => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ResultProvider<>);
        }
    }
}