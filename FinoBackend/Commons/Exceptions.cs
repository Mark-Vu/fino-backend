namespace FinoBackend.Common;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Forbidden") : base(message) { }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message = "Not found") : base(message) { }
}

public class BadRequestException : Exception
{
    public BadRequestException(string message = "Bad request") : base(message) { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Unauthorized") : base(message) { }
}