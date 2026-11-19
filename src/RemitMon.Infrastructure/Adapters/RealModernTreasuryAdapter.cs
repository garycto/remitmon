using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemitMon.Domain.Entities;
using RemitMon.Domain.Enums;

namespace RemitMon.Infrastructure.Adapters;

public sealed class RealModernTreasuryAdapter : IPaymentOrchestrationAdapter
{
    private readonly HttpClient _http;
    private readonly ModernTreasuryOptions _options;
    private readonly ILogger<RealModernTreasuryAdapter> _logger;

    public RealModernTreasuryAdapter(
        HttpClient http,
        IOptions<ModernTreasuryOptions> options,
        ILogger<RealModernTreasuryAdapter> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;

        ValidateOptions();
        _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task<IReadOnlyList<Counterparty>> ListCounterpartiesAsync(
        Guid businessCustomerId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync("api/counterparties?per_page=25", cancellationToken);
        using var document = await ReadJsonAsync(response, cancellationToken);
        var root = document.RootElement;
        var items = GetItems(root);
        var result = new List<Counterparty>(items.GetArrayLength());

        foreach (var item in items.EnumerateArray())
        {
            var id = GetString(item, "id");
            result.Add(new Counterparty
            {
                Id = Guid.TryParse(id, out var mtId) ? mtId : Guid.NewGuid(),
                BusinessCustomerId = businessCustomerId,
                Name = GetString(item, "name") ?? "Unnamed counterparty",
                Status = CounterpartyStatus.Pending,
                Source = CounterpartySource.Observed,
                RoutingAccountFingerprints = GetRoutingFingerprints(item),
                CreatedAt = ParseTimestamp(item, "created_at") ?? DateTime.UtcNow,
                UpdatedAt = ParseTimestamp(item, "updated_at") ?? DateTime.UtcNow
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<PaymentEvent>> ListPaymentOrdersAsync(
        Guid accountId,
        Guid? counterpartyId = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync("api/payment_orders?per_page=25", cancellationToken);
        using var document = await ReadJsonAsync(response, cancellationToken);
        var items = GetItems(document.RootElement);
        var result = new List<PaymentEvent>(items.GetArrayLength());
        foreach (var item in items.EnumerateArray())
            result.Add(MapPaymentOrder(item, accountId, counterpartyId));
        return result;
    }

    public async Task<IReadOnlyList<PaymentEvent>> ListIncomingPaymentDetailsAsync(
        Guid accountId,
        Guid? counterpartyId = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync("api/incoming_payment_details?per_page=25", cancellationToken);
        using var document = await ReadJsonAsync(response, cancellationToken);
        var items = GetItems(document.RootElement);
        var result = new List<PaymentEvent>(items.GetArrayLength());
        foreach (var item in items.EnumerateArray())
            result.Add(MapIncomingPaymentDetail(item, accountId, counterpartyId));
        return result;
    }

    public async Task<PaymentEvent?> GetPaymentOrderAsync(
        Guid accountId,
        string paymentOrderId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync($"api/payment_orders/{Uri.EscapeDataString(paymentOrderId)}", cancellationToken);
        using var document = await ReadJsonAsync(response, cancellationToken);
        return MapPaymentOrder(document.RootElement, accountId, null);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.OrganizationId))
        {
            if (_options.IsLive)
                throw new NotConfiguredException("Modern Treasury Live mode requires ModernTreasury:OrganizationId.");
        }
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            if (_options.IsLive)
                throw new NotConfiguredException("Modern Treasury Live mode requires ModernTreasury:ApiKey.");
        }

        _logger.LogInformation(
            "Modern Treasury configuration loaded. OrganizationId present: {OrganizationIdPresent}; ApiKey present: {ApiKeyPresent}; Live mode: {LiveMode}",
            !string.IsNullOrWhiteSpace(_options.OrganizationId),
            !string.IsNullOrWhiteSpace(_options.ApiKey),
            _options.IsLive);
    }

    private async Task<HttpResponseMessage> SendAsync(string relativeUri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.OrganizationId}:{_options.ApiKey}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Modern Treasury returned HTTP {StatusCode}", (int)response.StatusCode);
            throw new HttpRequestException(
                $"Modern Treasury returned HTTP {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }
        return response;
    }

    private async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static JsonElement GetItems(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
            return root;
        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            return data;
        if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
            return results;
        return JsonDocument.Parse("[]").RootElement.Clone();
    }

    private static PaymentEvent MapPaymentOrder(JsonElement item, Guid accountId, Guid? counterpartyId)
    {
        var amount = GetAmount(item, "amount");
        var direction = GetString(item, "direction");
        var type = GetString(item, "type");
        var eventTime = ParseTimestamp(item, "created_at") ?? ParseTimestamp(item, "effective_date") ?? DateTime.UtcNow;

        return new PaymentEvent
        {
            AccountId = accountId,
            ExternalEventId = GetString(item, "id"),
            Rail = MapRail(type),
            Direction = direction?.Equals("credit", StringComparison.OrdinalIgnoreCase) == true
                ? PaymentDirection.Outbound
                : PaymentDirection.Inbound,
            Initiation = PaymentInitiation.Push,
            Amount = amount,
            Currency = GetString(item, "currency")?.ToUpperInvariant() ?? "USD",
            CounterpartyRef = GetString(item, "counterparty") ?? GetNestedString(item, "receiving_account", "party_name"),
            CounterpartyId = counterpartyId,
            Reference = GetString(item, "reference_number") ?? GetString(item, "external_id"),
            Memo = GetString(item, "description"),
            InvoiceRef = GetString(item, "metadata_invoice_ref"),
            EventTime = eventTime,
            SettlementDate = ParseTimestamp(item, "effective_date"),
            RawPayload = item.Clone().EnumerateObject().ToDictionary(
                property => property.Name,
                property => (object)JsonElementToObject(property.Value)),
            CorrelationId = GetString(item, "id")
        };
    }

    private static PaymentEvent MapIncomingPaymentDetail(JsonElement item, Guid accountId, Guid? counterpartyId)
    {
        var type = GetString(item, "type");
        var eventTime = ParseTimestamp(item, "created_at") ?? ParseTimestamp(item, "as_of_date") ?? DateTime.UtcNow;

        return new PaymentEvent
        {
            AccountId = accountId,
            ExternalEventId = GetString(item, "id"),
            Rail = MapRail(type),
            Direction = PaymentDirection.Inbound,
            Initiation = PaymentInitiation.Pull,
            Amount = GetAmount(item, "amount"),
            Currency = GetString(item, "currency")?.ToUpperInvariant() ?? "USD",
            CounterpartyRef = GetNestedString(item, "data", "detail_record", "receiving_company_name")
                ?? GetNestedString(item, "data", "batch_header_record", "company_name"),
            CounterpartyId = counterpartyId,
            Reference = GetString(item, "transaction_id") ?? GetString(item, "id"),
            Memo = GetString(item, "status"),
            EventTime = eventTime,
            SettlementDate = ParseTimestamp(item, "as_of_date"),
            RawPayload = item.Clone().EnumerateObject().ToDictionary(
                property => property.Name,
                property => (object)JsonElementToObject(property.Value)),
            CorrelationId = GetString(item, "id")
        };
    }

    private static Rail MapRail(string? type)
        => type?.ToLowerInvariant() switch
        {
            "ach" => Rail.ACH,
            "rtp" => Rail.RTP,
            "fednow" => Rail.FedNow,
            "wire" => Rail.Wire,
            _ => Rail.ACH
        };

    private static decimal GetAmount(JsonElement item, string property)
    {
        if (!item.TryGetProperty(property, out var value))
            return 0m;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var decimalValue))
            return decimal.Round(decimalValue / 100m, 2);
        if (value.ValueKind == JsonValueKind.String
            && decimal.TryParse(value.GetString(), out decimalValue))
            return decimal.Round(decimalValue / 100m, 2);
        return 0m;
    }

    private static string? GetString(JsonElement item, string property)
        => item.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? GetNestedString(JsonElement item, params string[] properties)
    {
        var current = item;
        foreach (var property in properties)
        {
            if (!current.TryGetProperty(property, out var next))
                return null;
            current = next;
        }
        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }

    private static DateTime? ParseTimestamp(JsonElement item, string property)
    {
        var value = GetString(item, property);
        return value is not null && DateTime.TryParse(value, out var parsed)
            ? parsed.ToUniversalTime()
            : null;
    }

    private static List<string> GetRoutingFingerprints(JsonElement item)
    {
        var fingerprints = new List<string>();
        if (!item.TryGetProperty("accounts", out var accounts) || accounts.ValueKind != JsonValueKind.Array)
            return fingerprints;
        foreach (var account in accounts.EnumerateArray())
        {
            if (account.TryGetProperty("account_details", out var details)
                && details.ValueKind == JsonValueKind.Array)
            {
                foreach (var detail in details.EnumerateArray())
                {
                    var number = GetString(detail, "account_number");
                    if (!string.IsNullOrWhiteSpace(number))
                        fingerprints.Add($"****{number[^Math.Min(4, number.Length)..]}");
                }
            }
        }
        return fingerprints;
    }

    private static object JsonElementToObject(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? string.Empty,
        JsonValueKind.Number => element.TryGetDecimal(out var decimalValue) ? decimalValue : element.GetRawText(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null!,
        JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(
            property => property.Name,
            property => JsonElementToObject(property.Value)),
        _ => element.GetRawText()
    };
}
