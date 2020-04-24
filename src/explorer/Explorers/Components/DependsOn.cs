namespace Explorer.Explorers.Components
{
    internal interface DependsOn<TResult>
    {
        public void LinkToSourceComponent(ExplorerComponent<TResult> component);
    }
}
