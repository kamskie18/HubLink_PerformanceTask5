namespace HubLink.Application.Models;

public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; }
    public bool IsEmpty { get; set; }

    public static ApiResponse<T> Success(T data, int statusCode = 200, bool isEmpty = false) =>
        new()
        {
            IsSuccess = true,
            Data = data,
            StatusCode = statusCode,
            IsEmpty = isEmpty
        };

    public static ApiResponse<T> Failure(string errorMessage, int statusCode = 0) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            StatusCode = statusCode
        };
}
