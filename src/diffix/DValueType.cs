#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1720 // Identifier contains type name
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Diffix
{
    /// <summary>
    /// Represents the different data types an Aircloak column can take.
    /// </summary>
    public enum DValueType
    {
        Integer,
        Real,
        Text,
        Timestamp,
        Date,
        Datetime,
        Bool,
        Unknown,
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore CS1720 // Identifier contains type name
#pragma warning restore SA1602 // Enumeration items should be documented