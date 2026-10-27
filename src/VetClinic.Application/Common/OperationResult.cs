namespace VetClinic.Application.Common;

/// <summary>
/// Padrão Operation Result: todo service devolve sucesso ou falha explícita,
/// com o status HTTP sugerido. Os controllers convertem via BaseApiController.ToActionResult().
/// </summary>
public class OperationResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }
    public int StatusCode { get; }

    private OperationResult(bool isSuccess, T? value, Error? error, int statusCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        StatusCode = statusCode;
    }

    public static OperationResult<T> Success(T value, int statusCode = 200)
        => new(true, value, null, statusCode);

    public static OperationResult<T> Failure(Error error, int statusCode)
        => new(false, default, error, statusCode);
}
