#pragma warning disable CA1056 // Change the type of property ExploreParams.ApiUrl from string to System.Uri.

namespace Explorer.Api.Models
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;

    public class ExploreParams
    {
        private string apiUrl = string.Empty;

        [Required]
        public string ApiUrl
        {
            get => apiUrl;
            set => apiUrl = value.EndsWith("/", ignoreCase: false, CultureInfo.InvariantCulture) ? value : $"{value}/";
        }

        [Required]
        public string ApiKey { get; set; } = string.Empty;

        [Required]
        public string DataSource { get; set; } = string.Empty;

        [Required]
        public string Table { get; set; } = string.Empty;

        [NonEmpty]
        public ImmutableArray<string> Columns { get; set; } = ImmutableArray<string>.Empty;

        [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
        private class NonEmptyAttribute : ValidationAttribute
        {
            public override object TypeId => base.TypeId;

            public override bool RequiresValidationContext => base.RequiresValidationContext;

            public override bool IsValid(object value) => value is IEnumerable<object> e && e.Any();

            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                return IsValid(value)
                    ? ValidationResult.Success
                    : new ValidationResult($"Array '{validationContext.MemberName}' is required and cannot be empty.");
            }
        }
    }
}

#pragma warning restore CA1056 // Change the type of property ExploreParams.ApiUrl from string to System.Uri.