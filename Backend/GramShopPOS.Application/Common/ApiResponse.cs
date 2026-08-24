namespace GramShopPOS.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = [];

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) =>
        new()
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = errors?.ToList() ?? []
        };
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse OkMessage(string message = "Success") =>
        new() { Success = true, Message = message, Data = null };

    public static new ApiResponse Fail(string message, IEnumerable<string>? errors = null) =>
        new()
        {
            Success = false,
            Message = message,
            Data = null,
            Errors = errors?.ToList() ?? []
        };
}
