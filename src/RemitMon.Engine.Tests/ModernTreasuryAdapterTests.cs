using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RemitMon.Infrastructure.Adapters;
using Xunit;

namespace RemitMon.Engine.Tests;

public sealed class ModernTreasuryAdapterTests
{
    [Fact]
    public async Task MapsPaymentOrderResponse()
    {
        var handler = new StubHttpMessageHandler("""
            [
              {
                "id": "po-123",
                "type": "ach",
                "direction": "credit",
                "amount": 125025,
                "currency": "USD",
                "description": "Vendor payment",
                "external_id": "invoice-42",
                "created_at": "2026-11-18T10:00:00Z"
              }
            ]
            """);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://app.moderntreasury.com/") };
        var adapter = new RealModernTreasuryAdapter(
            client,
            Options.Create(new ModernTreasuryOptions
            {
                OrganizationId = "test-org",
                ApiKey = "test-key",
                BaseUrl = "https://app.moderntreasury.com",
                IntegrationsMode = "Live"
            }),
            NullLogger<RealModernTreasuryAdapter>.Instance);

        var payments = await adapter.ListPaymentOrdersAsync(Guid.NewGuid());

        var payment = Assert.Single(payments);
        Assert.Equal("po-123", payment.ExternalEventId);
        Assert.Equal(1250.25m, payment.Amount);
        Assert.Equal("USD", payment.Currency);
        Assert.Equal("invoice-42", payment.Reference);
    }

    [Fact]
    public void WebhookVerifierAcceptsValidSignature()
    {
        const string payload = "{\"event\":\"created\",\"data\":{\"id\":\"123\"}}";
        const string key = "test-webhook-key";
        var signature = Convert.ToHexString(
            System.Security.Cryptography.HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(key),
                Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

        Assert.True(new ModernTreasuryWebhookVerifier().Verify(payload, signature, key));
    }

    [Fact]
    public void WebhookVerifierRejectsInvalidSignature()
    {
        Assert.False(new ModernTreasuryWebhookVerifier().Verify(
            "{\"event\":\"created\"}",
            "invalid",
            "test-webhook-key"));
    }

    [Fact]
    public void LiveOptionsRejectMissingCredentials()
    {
        var options = new ModernTreasuryOptions
        {
            OrganizationId = null,
            ApiKey = null,
            BaseUrl = "https://app.moderntreasury.com",
            IntegrationsMode = "Live"
        };

        var results = options.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(options)).ToList();

        Assert.Contains(results, result => result.ErrorMessage?.Contains("OrganizationId") == true);
        Assert.Contains(results, result => result.ErrorMessage?.Contains("ApiKey") == true);
    }

    [Fact]
    public async Task AuthorizationHeaderDoesNotAppearInExceptionOrLogOutput()
    {
        var handler = new StubHttpMessageHandler("{}", HttpStatusCode.Unauthorized);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://app.moderntreasury.com/") };
        var logs = new List<string>();
        var logger = new CapturingLogger<RealModernTreasuryAdapter>(logs);
        var apiKey = "test-secret-key-value";
        var adapter = new RealModernTreasuryAdapter(
            client,
            Options.Create(new ModernTreasuryOptions
            {
                OrganizationId = "test-org",
                ApiKey = apiKey,
                BaseUrl = "https://app.moderntreasury.com",
                IntegrationsMode = "Live"
            }),
            logger);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => adapter.ListPaymentOrdersAsync(Guid.NewGuid()));

        Assert.DoesNotContain(apiKey, string.Join("\n", logs));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _body;
        private readonly HttpStatusCode _statusCode;

        public StubHttpMessageHandler(string body, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _body = body;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Assert.NotNull(request.Headers.Authorization);
            Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class CapturingLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        private readonly ICollection<string> _logs;

        public CapturingLogger(ICollection<string> logs) => _logs = logs;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _logs.Add(formatter(state, exception));
        }
    }
}
