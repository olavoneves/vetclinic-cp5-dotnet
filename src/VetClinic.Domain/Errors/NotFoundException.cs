namespace VetClinic.Domain.Errors;

/// <summary>Recurso não encontrado → HTTP 404.</summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string code, string message) : base(code, message) { }
}
