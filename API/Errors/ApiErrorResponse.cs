using Microsoft.AspNetCore.Mvc;

namespace API.Errors;

public class ApiErrorResponse : ProblemDetails
{
    /// <summary>Stable, unique-per-cause code the consumer can branch on (e.g. "PRODUCT_UPDATE_ERROR").</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Stack trace / exception detail. Only populated in Development.</summary>
    public string? Details { get; set; }

    public ApiErrorResponse(int statusCode, string? message = null, string? errorCode = null, string? details = null)
    {
        Status = statusCode;
        Title = message ?? DefaultMessageFor(statusCode);
        ErrorCode = errorCode;
        Details = details;
    }

    private static string DefaultMessageFor(int statusCode) => statusCode switch
    {
        400 => "Bad request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Resource not found",
        500 => "Internal server error",
        _ => "An error occurred"
    };
}
