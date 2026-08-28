using Microsoft.AspNetCore.Mvc;

namespace API.Errors;

[ApiController]
[Route("errors/{code:int}")]
public class ErrorController : ControllerBase
{
    public ActionResult Error(int code)
    {
        var errorCode = code == StatusCodes.Status404NotFound
            ? ErrorCodes.General.NotFound
            : ErrorCodes.General.ServerError;

        return StatusCode(code, new ApiErrorResponse(code, errorCode: errorCode));
    }
}
