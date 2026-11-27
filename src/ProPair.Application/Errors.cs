namespace ProPair.Application;

public sealed class NotFoundException(string what) : Exception($"{what} not found.");

public sealed class ForbiddenException(string message) : Exception(message);
