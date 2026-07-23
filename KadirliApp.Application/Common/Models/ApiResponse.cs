using System.Text.Json.Serialization;

namespace KadirliApp.Application.Common.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }

    // WhenWritingNull değer tiplerinde (ApiResponse<Guid>, ApiResponse<bool>) serileştirmeyi patlatır;
    // WhenWritingDefault referans tiplerde aynı davranır, değer tiplerinde geçerlidir.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T? Data { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Meta { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiError? Error { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, object? meta = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Meta = meta
        };
    }

    public static ApiResponse<T> FailureResponse(string code, string message, object? meta = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = new ApiError { Code = code, Message = message },
            Meta = meta
        };
    }
}

public class ApiError
{
    public string Code { get; set; } = default!;
    public string Message { get; set; } = default!;
}
