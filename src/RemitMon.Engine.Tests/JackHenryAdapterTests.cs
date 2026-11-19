using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RemitMon.Infrastructure.Adapters;
using Xunit;

namespace RemitMon.Engine.Tests;

public sealed class JackHenryAdapterTests
{
    [Fact]
    public async Task MapsJackHenryTransactionResponseAndUsesTokenAuthorization()
    {
        using var rsa = RSA.Create(2048);
        var privateKeyPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(privateKeyPath, rsa.ExportRSAPrivateKeyPem());

        try
        {
            var handler = new JackHenryStubHandler("""
                {
                  "transactions": [
                    {
                      "id": "txn-123",
                      "transaction_type": "ach_inbound_credit",
                      "amount": "125.50",
                      "currency": "USD",
                      "company_name": "Acme Supplier",
                      "client_reference_id": "INV-42",
                      "status": "Success",
                      "created_at": "1639507804049"
                    }
                  ]
                }
                """);
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://app.stage.victorfi.com/")
            };
            var adapter = new RealJackHenryAdapter(
                client,
                Options.Create(new JackHenryOptions
                {
                    ApiKey = "test-api-key",
                    PrivateKeyPath = privateKeyPath,
                    BaseUrl = "https://app.stage.victorfi.com",
                    IntegrationsMode = "Live",
                    TransactionsPath = "/api/transactions"
                }),
                NullLogger<RealJackHenryAdapter>.Instance);

            var payments = await adapter.ListIncomingPaymentDetailsAsync(Guid.NewGuid());

            var payment = Assert.Single(payments);
            Assert.Equal("txn-123", payment.ExternalEventId);
            Assert.Equal(125.50m, payment.Amount);
            Assert.Equal("ACH", payment.Rail.ToString());
            Assert.Equal("Inbound", payment.Direction.ToString());
            Assert.NotNull(handler.LastAuthorization);
            Assert.StartsWith("Token ", handler.LastAuthorization);
            Assert.DoesNotContain("test-api-key", handler.LastAuthorization);
        }
        finally
        {
            File.Delete(privateKeyPath);
        }
    }

    [Fact]
    public async Task MapsJackHenryAccountResponseWithMaskedNumber()
    {
        using var rsa = RSA.Create(2048);
        var privateKeyPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(privateKeyPath, rsa.ExportRSAPrivateKeyPem());
        try
        {
            var handler = new JackHenryStubHandler("""
                {
                  "content": [
                    {
                      "id": "acct-123",
                      "account_name": "USD Account",
                      "account_number": "9999999999",
                      "account_type": "Deposit",
                      "currency": "USD",
                      "available_balance": "1250.50",
                      "status": "Active"
                    }
                  ]
                }
                """);
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.stage.victorfi.com/")
            };
            var adapter = new RealJackHenryAdapter(
                client,
                Options.Create(new JackHenryOptions
                {
                    ApiKey = "test-api-key",
                    PrivateKeyPath = privateKeyPath,
                    BaseUrl = "https://api.stage.victorfi.com",
                    AccountsPath = "/v2/accounts",
                    IntegrationsMode = "Live"
                }),
                NullLogger<RealJackHenryAdapter>.Instance);

            var accounts = await adapter.ListAccountsAsync();
            var account = Assert.Single(accounts);
            Assert.Equal("acct-123", account.Id);
            Assert.Equal("USD Account", account.Name);
            Assert.Equal("****9999", account.MaskedAccountNumber);
            Assert.Equal(1250.50m, account.Balance);
        }
        finally
        {
            File.Delete(privateKeyPath);
        }
    }

    [Fact]
    public void LiveOptionsRejectMissingCredentials()
    {
        var options = new JackHenryOptions
        {
            ApiKey = null,
            PrivateKeyPath = null,
            BaseUrl = "https://app.stage.victorfi.com",
            IntegrationsMode = "Live"
        };

        var results = options.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(options)).ToList();

        Assert.Contains(results, result => result.ErrorMessage?.Contains("ApiKey") == true);
        Assert.Contains(results, result => result.ErrorMessage?.Contains("PrivateKeyPath") == true);
    }

    [Fact]
    public void WebhookVerifierAcceptsPassAndRejectsFail()
    {
        const string payload = "{\"transaction_type\":\"ach_inbound_credit\"}";
        const string key = "test-jh-webhook-key";
        var signature = Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

        Assert.True(new ModernTreasuryWebhookVerifier().Verify(payload, signature, key));
        Assert.False(new ModernTreasuryWebhookVerifier().Verify(payload, "bad", key));
    }

    private sealed class JackHenryStubHandler : HttpMessageHandler
    {
        private readonly string _body;

        public JackHenryStubHandler(string body) => _body = body;
        public string? LastAuthorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastAuthorization = request.Headers.Authorization?.ToString();
            Assert.StartsWith("Token ", LastAuthorization);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
        }
    }
}
