namespace OpsPulse.Application.Security;

public sealed record ApiKeyDto(
    Guid Id,
    string Name,
    string KeyPrefix,
    bool IsActive,
    DateTime? LastUsedAtUtc,
    DateTime CreatedAtUtc);

/// <summary>The raw key is returned exactly once, at creation time.</summary>
public sealed record ApiKeyCreatedDto(ApiKeyDto ApiKey, string Key);

public sealed record CreateApiKeyRequest(string Name);
