namespace Explorer.Queries
{
    public static class GroupingId
    {
        public static int FromIndex(int index) => ~(1 << index);

        public static int FromIndices(int[] indices)
        {
            var result = 0;
            foreach (var i in indices)
            {
                result |= FromIndex(i);
            }
            return result;
        }
    }
}