namespace Explorer.Common.Utils
{
    using System;
    using System.Collections.Generic;

    using SubstringWithCountList = ValueWithCountList<string>;

    /// <summary>
    /// Stores the substrings at each position in a column,
    /// together with the number of occurences (counts) for each substring.
    /// </summary>
    internal class SubstringsData
    {
        public SubstringsData()
        {
            Substrings = new List<SubstringWithCountList>();
        }

        public int Count => Substrings.Count;

        private List<SubstringWithCountList> Substrings { get; }

        public void Add(int pos, string s, long count)
        {
            while (Substrings.Count <= pos)
            {
                Substrings.Add(new SubstringWithCountList());
            }
            Substrings[pos].AddValueCount(s, count);
        }

        public string? GetRandomSubstring(int pos, Random rand)
        {
            return Substrings[pos].GetRandomValue(rand);
        }
    }
}
