using API.Errors;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BaseApiController : ControllerBase
{
    protected ActionResult ApiOk<T>(T? data, string? message = null)
    {
        if (data == null) return ApiError(StatusCodes.Status404NotFound, errorCode: ErrorCodes.General.NotFound);

        return Ok(new ApiResponse<T>(StatusCodes.Status200OK, data, message));
    }

    protected ActionResult ApiCreated<T>(T data, string actionName, object routeValues, string? message = null)
    {
        return CreatedAtAction(actionName, routeValues,
            new ApiResponse<T>(StatusCodes.Status201Created, data, message));
    }

    protected ActionResult ApiError(int statusCode, string? message = null, string? errorCode = null)
    {
        return StatusCode(statusCode, new ApiErrorResponse(statusCode, message, errorCode));
    }
}