namespace Explorer.Explorers
{
    public static class TextColumnExplorer
    {
        public static Exploration TextExploration(Lamar.INestedContainer scope) =>
            Exploration.Compose(scope, _ =>
            {
                _.AddPublisher<Components.DistinctValuesComponent>();
                _.AddPublisher<Components.EmailCheckComponent>();
                _.AddPublisher<Components.TextGeneratorComponent>();
                _.AddPublisher<Components.TextLengthComponent>();
            });
    }
}
