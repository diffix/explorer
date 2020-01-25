#pragma warning disable CA1308 // replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'

namespace Aircloak.JsonApi
{
    using System.Linq;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Helper class for converting Json keys from the .NET standard PascalCase to snake_case.
    /// </summary>
    internal class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string pascalCase)
        {
            var fragments = Regex.Matches(pascalCase, "[A-Z]+[a-z]+")
                .Select(match => match.Value.ToLowerInvariant());
            return string.Join("_", fragments);
        }
    }
}

#pragma warning restore CA1308 // replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'