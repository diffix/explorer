namespace Explorer.Metrics
{
    using Accord.Statistics.Testing;

    public sealed class GoodnessMetric
    {
        private GoodnessMetric(string method, double pValue, bool significant, int rank)
        {
            Method = method;
            PValue = pValue;
            Significant = significant;
            Rank = rank;
        }

        public string Method { get; }

        public double PValue { get; }

        public bool Significant { get; }

        public int Rank { get; }

        public static GoodnessMetric AndersonDarling(AndersonDarlingTest ad, int rank) =>
            new GoodnessMetric("AndersonDarling", ad.PValue, ad.Significant, rank);

        public static GoodnessMetric ChiSquare(ChiSquareTest cs, int rank) =>
            new GoodnessMetric("ChiSquare", cs.PValue, cs.Significant, rank);

        public static GoodnessMetric KolmogorovSmirnov(KolmogorovSmirnovTest ks, int rank) =>
            new GoodnessMetric("KolmogorovSmirnov", ks.PValue, ks.Significant, rank);
    }
}