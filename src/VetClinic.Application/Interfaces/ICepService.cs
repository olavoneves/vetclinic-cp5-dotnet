using VetClinic.Application.Common;
using VetClinic.Application.DTOs;

namespace VetClinic.Application.Interfaces;

/// <summary>Serviço externo de consulta de CEP (implementado com ViaCEP na Infrastructure).</summary>
public interface ICepService
{
    Task<OperationResult<EnderecoDto>> GetAddressAsync(string cep, CancellationToken ct = default);
}
