using System.Security.Cryptography;
using System.Text;

namespace PaymentApi.Services;

/// <summary>
/// Service for generating immutable IDs that cannot be changed once created.
/// Provides additional validation and audit capabilities.
/// </summary>
public sealed class ImmutableIdService
{
    private readonly ILogger<ImmutableIdService> _logger;

    public ImmutableIdService(ILogger<ImmutableIdService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates a new immutable billing ID with timestamp and random components.
    /// Format: BILL_{timestamp}_{random}
    /// </summary>
    public string GenerateBillingId()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = GenerateSecureRandom(8);
        var billingId = $"BILL_{timestamp}_{random}";
        
        _logger.LogInformation("Generated new immutable billing ID: {BillingId}", billingId);
        return billingId;
    }

    /// <summary>
    /// Generates a new immutable session ID with timestamp and random components.
    /// Format: SESS_{timestamp}_{random}
    /// </summary>
    public string GenerateSessionId()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = GenerateSecureRandom(8);
        var sessionId = $"SESS_{timestamp}_{random}";
        
        _logger.LogInformation("Generated new immutable session ID: {SessionId}", sessionId);
        return sessionId;
    }

    /// <summary>
    /// Validates that a billing ID follows the immutable format.
    /// Accepts both new format (BILL_{timestamp}_{random}) and existing UUIDs for backward compatibility.
    /// </summary>
    public bool IsValidBillingId(string billingId)
    {
        if (string.IsNullOrWhiteSpace(billingId))
            return false;

        // Check if it's a UUID (existing format) - allow for backward compatibility
        if (Guid.TryParse(billingId, out _))
        {
            _logger.LogInformation("Billing ID {BillingId} is valid UUID format (legacy)", billingId);
            return true;
        }

        // Check new format: BILL_{timestamp}_{random}
        var parts = billingId.Split('_');
        if (parts.Length != 3 || parts[0] != "BILL")
            return false;

        // Validate timestamp is numeric
        if (!long.TryParse(parts[1], out var timestamp))
            return false;

        // Validate random part is alphanumeric and correct length
        if (parts[2].Length != 8 || !parts[2].All(c => char.IsLetterOrDigit(c)))
            return false;

        _logger.LogInformation("Billing ID {BillingId} is valid new immutable format", billingId);
        return true;
    }

    /// <summary>
    /// Validates that a session ID follows the immutable format.
    /// </summary>
    public bool IsValidSessionId(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        // Check format: SESS_{timestamp}_{random}
        var parts = sessionId.Split('_');
        if (parts.Length != 3 || parts[0] != "SESS")
            return false;

        // Validate timestamp is numeric
        if (!long.TryParse(parts[1], out var timestamp))
            return false;

        // Validate random part is alphanumeric and correct length
        if (parts[2].Length != 8 || !parts[2].All(c => char.IsLetterOrDigit(c)))
            return false;

        return true;
    }

    /// <summary>
    /// Generates a secure random string of specified length.
    /// </summary>
    private static string GenerateSecureRandom(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        
        var result = new StringBuilder(length);
        foreach (var b in bytes)
        {
            result.Append(chars[b % chars.Length]);
        }
        
        return result.ToString();
    }

    /// <summary>
    /// Logs an attempt to modify an immutable ID for audit purposes.
    /// </summary>
    public void LogImmutableIdModificationAttempt(string idType, string idValue, string operation)
    {
        _logger.LogWarning("IMMUTABLE_ID_MODIFICATION_ATTEMPT: {IdType}={IdValue}, Operation={Operation}", 
            idType, idValue, operation);
    }
}
