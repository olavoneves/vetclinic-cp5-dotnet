using VetClinic.API.Common;
using VetClinic.Domain.Errors;

namespace VetClinic.API.Middleware;

/// <summary>
/// Captura as exceptions lançadas nas camadas internas (ERRO CUSTOMIZADO - LANÇAMENTO)
/// e devolve ProblemDetails com o status correto: 404 / 409 / 422 / 400 / 500.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            BusinessRuleException => StatusCodes.Status422UnprocessableEntity,
            DomainException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        string code, detail;
        if (exception is DomainException domainException)
        {
            code = domainException.Code;
            detail = domainException.Message;
            _logger.LogWarning("Erro de domínio {Code}: {Message}", code, detail);
        }
        else
        {
            code = "Erro.Interno";
            detail = "Ocorreu um erro inesperado. Tente novamente mais tarde.";
            _logger.LogError(exception, "Erro não tratado em {Path}", context.Request.Path);
        }

        if (context.Response.HasStarted)
            return;

        var problem = ProblemFactory.Create(context, statusCode, code, detail);
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
}
