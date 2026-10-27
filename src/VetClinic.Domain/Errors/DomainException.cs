namespace VetClinic.Domain.Errors;

/// <summary>
/// Exceção base de domínio. O middleware global da API converte cada subtipo
/// no status HTTP correspondente (ProblemDetails).
/// </summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}
