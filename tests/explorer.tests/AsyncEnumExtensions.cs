namespace Explorer.Tests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public static class AsyncEnumExtensions
    {
        public static async Task<List<T>> Collect<T>(this IAsyncEnumerable<T> asyncEnum)
        {
            var list = new List<T>();
            await foreach (var item in asyncEnum)
            {
                list.Add(item);
            }
            return list;
        }
    }
}