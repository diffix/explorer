namespace Explorer
{
    using System;
    using System.Threading.Tasks;
    using Lamar;

    public sealed class Exploration
    {
        internal Exploration(Task task)
        {
            Completion = task;
        }

        public Task Completion { get; }

        public static Exploration Compose(INestedContainer scope, Action<ExplorationComposer> compose)
        {
            var tasks = new ExplorationComposer(scope);
            compose(tasks);
            return tasks.Finalize();
        }
    }
}