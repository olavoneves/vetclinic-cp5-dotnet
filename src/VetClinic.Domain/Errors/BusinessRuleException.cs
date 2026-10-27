namespace VetClinic.Domain.Errors;

/// <summary>Violação de regra de negócio → HTTP 422.</summary>
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string code, string message) : base(code, message) { }
}
