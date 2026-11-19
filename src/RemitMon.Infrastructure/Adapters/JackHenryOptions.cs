using System.ComponentModel.DataAnnotations;

namespace RemitMon.Infrastructure.Adapters;

public sealed class JackHenryOptions : IValidatableObject
{
    public const string SectionName = "JackHenry";

    public string BaseUrl { get; set; } = "https://api.stage.victorfi.com";
    public string? ApiKey { get; set; }
    public string? PrivateKeyPath { get; set; }
    public string? WebhookSigningKey { get; set; }
    public string CounterpartiesPath { get; set; } = "/api/counterparties";
    public string TransactionsPath { get; set; } = "/api/transactions";
    public string PaymentsPath { get; set; } = "/api/payments";
    public string AccountsPath { get; set; } = "/v2/accounts";
    public string? IntegrationsMode { get; set; }

    public bool IsLive => string.Equals(IntegrationsMode, "Live", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme != Uri.UriSchemeHttps)
        {
            yield return new ValidationResult("JackHenry:BaseUrl must be a valid HTTPS URL.");
        }

        if (IsLive)
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                yield return new ValidationResult("JackHenry:ApiKey is required when Integrations:Mode is Live.");
            if (string.IsNullOrWhiteSpace(PrivateKeyPath))
                yield return new ValidationResult("JackHenry:PrivateKeyPath is required when Integrations:Mode is Live.");
        }
    }
}
