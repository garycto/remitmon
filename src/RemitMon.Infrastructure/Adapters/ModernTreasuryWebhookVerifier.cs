using System.Security.Cryptography;
using System.Text;

namespace RemitMon.Infrastructure.Adapters;

public sealed class ModernTreasuryWebhookVerifier
{
    public bool Verify(string payload, string signature, string signingKey)
    {
        if (string.IsNullOrWhiteSpace(payload)
            || string.IsNullOrWhiteSpace(signature)
            || string.IsNullOrWhiteSpace(signingKey))
            return false;

        var expected = Convert.ToHexString(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(signingKey),
                Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        var normalizedSignature = signature.Trim();

        if (expected.Length != normalizedSignature.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(normalizedSignature));
    }
}
