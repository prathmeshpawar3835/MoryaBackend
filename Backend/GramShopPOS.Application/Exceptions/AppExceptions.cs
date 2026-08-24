namespace GramShopPOS.Application.Exceptions;

public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public IReadOnlyList<string> Errors { get; }

    protected AppException(string message, int statusCode, IEnumerable<string>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors?.ToList() ?? [];
    }
}

public sealed class ValidationAppException : AppException
{
    public ValidationAppException(string message, IEnumerable<string>? errors = null)
        : base(message, 400, errors)
    {
    }
}

public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message = "Unauthorized.")
        : base(message, 401)
    {
    }
}

public sealed class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string message = "You are not allowed to perform this action.")
        : base(message, 403)
    {
    }
}

public sealed class NotFoundAppException : AppException
{
    public NotFoundAppException(string message = "Resource not found.")
        : base(message, 404)
    {
    }
}

public sealed class ConflictAppException : AppException
{
    public ConflictAppException(string message, IEnumerable<string>? errors = null)
        : base(message, 409, errors)
    {
    }
}

public class BusinessAppException : AppException
{
    public BusinessAppException(string message, IEnumerable<string>? errors = null)
        : base(message, 422, errors)
    {
    }
}

public sealed class InsufficientStockException : BusinessAppException
{
    public InsufficientStockException(string message = "Insufficient stock for one or more items.")
        : base(message)
    {
    }
}
