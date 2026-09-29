namespace Its.Domain.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string message, string code = "DOMAIN_ERROR") : base(message)
    {
        Code = code;
    }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key) 
        : base($"{entityName} with key '{key}' was not found.", "NOT_FOUND") { }
}

public class ConflictException : DomainException
{
    public ConflictException(string message) 
        : base(message, "CONFLICT") { }
}

public class ValidationException : DomainException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IDictionary<string, string[]>? errors = null) 
        : base(message, "VALIDATION_FAILED")
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }
}

public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message = "Unauthorized access.") 
        : base(message, "UNAUTHORIZED") { }
}

public class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.") 
        : base(message, "FORBIDDEN") { }
}
