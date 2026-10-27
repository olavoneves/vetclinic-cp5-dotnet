using Microsoft.AspNetCore.Mvc;
using VetClinic.API.Common;
using VetClinic.Application.Common;

namespace VetClinic.API.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Helper único que converte OperationResult em IActionResult.
    /// Falha → ProblemDetails com o status do resultado; sucesso → onSuccess ou status padrão.
    /// </summary>
    protected IActionResult ToActionResult<T>(OperationResult<T> result, Func<T, IActionResult>? onSuccess = null)
    {
        if (!result.IsSuccess)
        {
            var problem = ProblemFactory.Create(HttpContext, result.StatusCode, result.Error!.Code, result.Error.Message);
            return new ObjectResult(problem)
            {
                StatusCode = result.StatusCode,
                ContentTypes = { "application/problem+json" }
            };
        }

        if (onSuccess is not null)
            return onSuccess(result.Value!);

        return result.StatusCode == StatusCodes.Status204NoContent
            ? NoContent()
            : StatusCode(result.StatusCode, result.Value);
    }
}
