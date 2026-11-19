using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemitMon.Domain.Entities;
using RemitMon.Domain.Enums;

namespace RemitMon.Infrastructure.Adapters;

public sealed record JackHenryAccount(
    string Id,
    string Name,
    string? MaskedAccountNumber,
    string? AccountType,
    string Currency,
    decimal? Balance,
    string Status);

public sealed class RealJackHenryAdapter : IPaymentOrchestrationAdapter
{
    private readonly HttpClient _http;
    private readonly JackHenryOptions _options;
    private readonly ILogger<RealJackHenryAdapter> _logger;
    private readonly object _tokenLock = new();
    private RSA? _rsa;
    private string? _token;
    private DateTimeOffset _tokenExpiresAt;

    public RealJackHenryAdapter(
        HttpClient http,
        IOptions<JackHenryOptions> options,
        ILogger<RealJackHenryAdapter> logger)
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
        using var response = await SendAsync(_options.CounterpartiesPath, cancellationToken);
        using var document = await ReadJsonAsync(response, cancellationToken);
        var items = GetItems(document.RootElement);
        var result = new List<Counterparty>(items.GetArrayLength());

        foreach (var item in items.EnumerateArray())
        {
            var id = GetString(item, "id") ?? GetString(item, "counterparty_id");
            var accountName = GetString(item, "account_name")
                ?? GetString(item, "name")
                ?? "Unnamed counterparty";
            result.Add(new Counterparty
            {
                Id = Guid.TryParse(id, out var parsed) ? parsed : Guid.NewGuid(),
                BusinessCustomerId = businessCustomerId,
                Name = accountName,
                Status = CounterpartyStatus.Pending,
                Source = CounterpartySource.Observed,
                RoutingAccountFingerprints = GetRoutingFingerprints(item),
                CreatedAt = ParseUnixTimestamp(item, "created_at") ?? DateTime.UtcNow,
                UpdatedAt = ParseUnixTimestamp(item, "updated_at") ?? DateTime.UtcNow
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<JackHenryAccount>> ListAccountsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(_options.AccountsPath, cancellationToken);
        using var document = await ReadJsonAsync(response, cancellationToken);
        var items = GetItems(document.RootElement);
        var result = new List<JackHenryAccount>(items.GetArrayLength());

        foreach (var item in items.EnumerateArray())
        {
            var id = GetString(item, "id")
                ?? GetString(item, "victorAccountId")
                ?? Guid.NewGuid().ToString();
            var name = GetString(item, "name")
                ?? GetString(item, "account_name")
                ?? "Unnamed account";
            var accountNumber = GetString(item, "account_number")
                ?? GetString(item, "masked_account_number");
            var masked = MaskAccountNumber(accountNumber);
            var accountType = GetString(item, "account_type")
                ?? GetString(item, "type");
            var currency = GetString(item, "currency")?.ToUpperInvariant() ?? "USD";
            var status = GetString(item, "status") ?? "Active";
            var balance = GetOptionalAmount(item);

            result.Add(new JackHenryAccount(
                id,
                name,
                masked,
                accountType,
                currency,
                balance,
                status));
        }

        return result;
    }

    public async Task<IReadOnlyList<PaymentEvent>> ListPaymentOrdersAsync(
        Guid accountId,
        Guid? counterpartyId = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(_options.PaymentsPath, cancellationToken);
        using var document = await ReadJsonAsync(response, cancellationToken);
        return MapTransactions(document.RootElement, accountId, counterpartyId);
    }

    public async Task<IReadOnlyList<PaymentEvent>> ListIncomingPaymentDetailsAsync(
        Guid accountId,
        Guid? counterpartyId = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync($"{_options.TransactionsPath}?direction=inbound", cancellationToken);
        using var document = await ReadJsonAsync(response, cancellationToken);
        return MapTransactions(document.RootElement, accountId, counterpartyId);
    }

    public async Task<PaymentEvent?> GetPaymentOrderAsync(
        Guid accountId,
        string paymentOrderId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            $"{_options.PaymentsPath.TrimEnd('/')}/{Uri.EscapeDataString(paymentOrderId)}",
            cancellationToken);
        using var document = await ReadJsonAsync(response, cancellationToken);
        var item = document.RootElement.TryGetProperty("data", out var data)
            ? data
            : document.RootElement;
        return MapTransactions(JsonDocument.Parse($"[{item.GetRawText()}]").RootElement, accountId, null).FirstOrDefault();
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) && _options.IsLive)
            throw new NotConfiguredException("Jack Henry Live mode requires JackHenry:ApiKey.");
        if (string.IsNullOrWhiteSpace(_options.PrivateKeyPath) && _options.IsLive)
            throw new NotConfiguredException("Jack Henry Live mode requires JackHenry:PrivateKeyPath.");

        _logger.LogInformation(
            "Jack Henry configuration loaded. ApiKey present: {ApiKeyPresent}; PrivateKey present: {PrivateKeyPresent}; Live mode: {LiveMode}",
            !string.IsNullOrWhiteSpace(_options.ApiKey),
            !string.IsNullOrWhiteSpace(_options.PrivateKeyPath),
            _options.IsLive);
    }

    private async Task<HttpResponseMessage> SendAsync(string relativeUri, CancellationToken cancellationToken)
    {
        var token = GetOrCreateToken();
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Token", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Jack Henry returned HTTP {StatusCode}", (int)response.StatusCode);
            throw new HttpRequestException(
                $"Jack Henry returned HTTP {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }
        return response;
    }

    private string GetOrCreateToken()
    {
        lock (_tokenLock)
        {
            if (!string.IsNullOrWhiteSpace(_token) && DateTimeOffset.UtcNow < _tokenExpiresAt)
                return _token;

            if (_options.IsLive && (string.IsNullOrWhiteSpace(_options.ApiKey)
                || string.IsNullOrWhiteSpace(_options.PrivateKeyPath)))
                throw new NotConfiguredException("Jack Henry Live mode credentials are not configured.");

            if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.PrivateKeyPath))
                throw new NotConfiguredException("Jack Henry credentials are not configured.");

            _rsa ??= RSA.Create();
            _rsa.ImportFromPem(File.ReadAllText(_options.PrivateKeyPath));

            var now = DateTimeOffset.UtcNow;
            var header = JsonSerializer.SerializeToUtf8Bytes(new
            {
                alg = "RS256",
                typ = "JWT"
            });
            var payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                sub = _options.ApiKey,
                iss = _options.ApiKey,
                iat = now.ToUnixTimeSeconds(),
                exp = now.AddMinutes(5).ToUnixTimeSeconds(),
                jti = Guid.NewGuid().ToString()
            });
            var encodedHeader = Base64Url(header);
            var encodedPayload = Base64Url(payload);
            var signingInput = Encoding.ASCII.GetBytes($"{encodedHeader}.{encodedPayload}");
            var signature = _rsa.SignData(signingInput, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            _token = $"{encodedHeader}.{encodedPayload}.{Base64Url(signature)}";
            _tokenExpiresAt = now.AddMinutes(4);
            return _token;
        }
    }

    private static string Base64Url(byte[] value)
        => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

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
        if (root.TryGetProperty("transactions", out var transactions) && transactions.ValueKind == JsonValueKind.Array)
            return transactions;
        if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
            return results;
        if (root.TryGetProperty("accounts", out var accounts) && accounts.ValueKind == JsonValueKind.Array)
            return accounts;
        if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            return content;
        return JsonDocument.Parse("[]").RootElement.Clone();
    }

    private static IReadOnlyList<PaymentEvent> MapTransactions(
        JsonElement root,
        Guid accountId,
        Guid? counterpartyId)
    {
        var items = GetItems(root);
        var result = new List<PaymentEvent>(items.GetArrayLength());
        foreach (var item in items.EnumerateArray())
        {
            var transactionType = GetString(item, "transaction_type")
                ?? GetString(item, "type");
            var amountText = GetString(item, "amount");
            var amount = decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                ? decimal.Round(parsed, 2)
                : 0m;

            var directionText = GetString(item, "direction");
            var inbound = directionText?.Equals("inbound", StringComparison.OrdinalIgnoreCase) == true
                || transactionType?.Contains("inbound", StringComparison.OrdinalIgnoreCase) == true
                || transactionType?.Contains("credit", StringComparison.OrdinalIgnoreCase) == true;

            var id = GetString(item, "id") ?? GetString(item, "transaction_id");
            result.Add(new PaymentEvent
            {
                AccountId = accountId,
                ExternalEventId = id,
                Rail = MapRail(transactionType),
                Direction = inbound ? PaymentDirection.Inbound : PaymentDirection.Outbound,
                Initiation = transactionType?.Contains("return", StringComparison.OrdinalIgnoreCase) == true
                    ? PaymentInitiation.Pull
                    : PaymentInitiation.Push,
                Amount = amount,
                Currency = GetString(item, "currency")?.ToUpperInvariant() ?? "USD",
                CounterpartyRef = GetString(item, "company_name")
                    ?? GetString(item, "originator_name")
                    ?? GetString(item, "account_name"),
                CounterpartyId = counterpartyId,
                Reference = GetString(item, "client_reference_id") ?? GetString(item, "reference"),
                Memo = GetString(item, "description") ?? GetString(item, "status"),
                EventTime = ParseUnixTimestamp(item, "created_at")
                    ?? ParseTimestamp(item, "created_at")
                    ?? DateTime.UtcNow,
                SettlementDate = ParseUnixTimestamp(item, "completed_at")
                    ?? ParseTimestamp(item, "completed_at"),
                RawPayload = item.Clone().EnumerateObject().ToDictionary(
                    property => property.Name,
                    property => (object)JsonElementToObject(property.Value)),
                CorrelationId = id
            });
        }
        return result;
    }

    private static Rail MapRail(string? transactionType)
        => transactionType?.ToLowerInvariant() switch
        {
            var value when value?.Contains("wire") == true => Rail.Wire,
            var value when value?.Contains("rtp") == true || value?.Contains("fednow") == true => Rail.RTP,
            var value when value?.Contains("ach") == true => Rail.ACH,
            _ => Rail.ACH
        };

    private static string? GetString(JsonElement item, string property)
        => item.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static decimal? GetOptionalAmount(JsonElement item)
    {
        foreach (var property in new[] { "available_balance", "balance", "ledger_balance" })
        {
            if (!item.TryGetProperty(property, out var value))
                continue;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
                return decimal.Round(number, 2);
            if (value.ValueKind == JsonValueKind.String
                && decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out number))
                return decimal.Round(number, 2);
        }
        return null;
    }

    private static string? MaskAccountNumber(string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            return null;
        var visible = accountNumber.Length <= 4
            ? accountNumber
            : accountNumber[^4..];
        return $"****{visible}";
    }

    private static List<string> GetRoutingFingerprints(JsonElement item)
    {
        var values = new List<string>();
        foreach (var property in new[] { "account_number", "external_account_number" })
        {
            var accountNumber = GetString(item, property);
            if (!string.IsNullOrWhiteSpace(accountNumber))
                values.Add($"****{accountNumber[^Math.Min(4, accountNumber.Length)..]}");
        }
        return values;
    }

    private static DateTime? ParseTimestamp(JsonElement item, string property)
    {
        var value = GetString(item, property);
        return value is not null && DateTime.TryParse(value, out var parsed)
            ? parsed.ToUniversalTime()
            : null;
    }

    private static DateTime? ParseUnixTimestamp(JsonElement item, string property)
    {
        if (!item.TryGetProperty(property, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.String
            && long.TryParse(value.GetString(), out var millis))
            return DateTimeOffset.FromUnixTimeMilliseconds(millis).UtcDateTime;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
            return number > 10_000_000_000
                ? DateTimeOffset.FromUnixTimeMilliseconds(number).UtcDateTime
                : DateTimeOffset.FromUnixTimeSeconds(number).UtcDateTime;
        return null;
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
