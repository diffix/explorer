namespace Aircloak.JsonApi
{
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Helper class for converting Json keys from the .NET standard PascalCase to snake_case.
    /// </summary>
    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string pascalCase)
        {
            var fragments = Regex.Matches(pascalCase, "[A-Z]+[a-z]+")
                .Select(match => match.Value.ToLower());
            return string.Join("_", fragments);
        }
    }

}