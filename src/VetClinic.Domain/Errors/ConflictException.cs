namespace VetClinic.Domain.Errors;

/// <summary>Conflito de estado (ex.: e-mail duplicado) → HTTP 409.</summary>
public class ConflictException : DomainException
{
    public ConflictException(string code, string message) : base(code, message) { }
}
