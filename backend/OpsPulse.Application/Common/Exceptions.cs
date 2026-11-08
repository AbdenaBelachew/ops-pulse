namespace OpsPulse.Application.Common;

public sealed class NotFoundException(string message) : Exception(message);

public sealed class ForbiddenException(string message) : Exception(message);

public sealed class ConflictException(string message) : Exception(message);

public sealed class UnauthorizedException(string message) : Exception(message);
