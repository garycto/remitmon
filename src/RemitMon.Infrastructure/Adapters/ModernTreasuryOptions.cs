using System.ComponentModel.DataAnnotations;

namespace RemitMon.Infrastructure.Adapters;

public sealed class ModernTreasuryOptions : IValidatableObject
{
    public const string SectionName = "ModernTreasury";

    [Required]
    public string BaseUrl { get; set; } = "https://app.moderntreasury.com";

    public string? OrganizationId { get; set; }
    public string? ApiKey { get; set; }
    public string? WebhookSigningKey { get; set; }

    public string? IntegrationsMode { get; set; }

    public bool IsLive => string.Equals(IntegrationsMode, "Live", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp))
        {
            yield return new ValidationResult("ModernTreasury:BaseUrl must be a valid absolute URL.");
        }

        if (IsLive)
        {
            if (string.IsNullOrWhiteSpace(OrganizationId))
                yield return new ValidationResult("ModernTreasury:OrganizationId is required when Integrations:Mode is Live.");
            if (string.IsNullOrWhiteSpace(ApiKey))
                yield return new ValidationResult("ModernTreasury:ApiKey is required when Integrations:Mode is Live.");
        }
    }
}
