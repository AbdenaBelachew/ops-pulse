namespace OpsPulse.API.Contracts;

public sealed record ApiHealthResponse(
    string Service,
    string Version,
    string Environment,
    DateTime TimestampUtc);
