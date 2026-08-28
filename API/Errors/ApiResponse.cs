namespace API.Errors;

public class ApiResponse<T>(int statusCode, T? data = default, string? message = null)
{
    public int StatusCode { get; set; } = statusCode;
    public string Message { get; set; } = message ?? DefaultMessageFor(statusCode);
    public T? Data { get; set; } = data;

    private static string DefaultMessageFor(int statusCode) => statusCode switch
    {
        200 => "Request successful",
        201 => "Resource created successfully",
        204 => "Request successful, no content",
        _ => "Success"
    };
}
