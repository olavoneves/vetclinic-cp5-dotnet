namespace VetClinic.Application.Common;

/// <summary>Erro de negócio retornado dentro de um <see cref="OperationResult{T}"/>.</summary>
public sealed record Error(string Code, string Message);
