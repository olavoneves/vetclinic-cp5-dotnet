using Microsoft.AspNetCore.Mvc;

namespace VetClinic.API.Common;

/// <summary>Monta o JSON de erro padronizado (ProblemDetails) usado por controllers e middleware.</summary>
public static class ProblemFactory
{
    public static ProblemDetails Create(HttpContext context, int statusCode, string code, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = TitleFor(statusCode),
            Detail = detail,
            Type = $"https://httpstatuses.io/{statusCode}",
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        return problem;
    }

    private static string TitleFor(int statusCode) => statusCode switch
    {
        400 => "Requisição inválida",
        401 => "Não autenticado",
        403 => "Acesso negado",
        404 => "Recurso não encontrado",
        409 => "Conflito",
        422 => "Regra de negócio violada",
        503 => "Serviço externo indisponível",
        _ => "Erro interno"
    };
}
