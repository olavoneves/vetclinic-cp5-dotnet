using VetClinic.Application.Common;
using VetClinic.Application.DTOs;
using VetClinic.Domain.Entities;

namespace VetClinic.Application.Interfaces;

// Fica em Application (e não em Domain) porque retorna OperationResult<T>,
// e o Domain não pode depender de nenhuma outra camada.
public interface ITutorRepository
{
    /// <summary>ERRO CUSTOMIZADO - RETORNO: devolve Failure(404) quando não existe.</summary>
    Task<OperationResult<Tutor>> GetByIdAsync(int id, CancellationToken ct = default);

    Task<PageResult<TutorResponse>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default);

    /// <summary>ERRO CUSTOMIZADO - LANÇAMENTO: lança ConflictException se o e-mail já existir.</summary>
    Task AddAsync(Tutor tutor, CancellationToken ct = default);

    /// <summary>ERRO CUSTOMIZADO - LANÇAMENTO: lança ConflictException se o e-mail pertencer a outro tutor.</summary>
    Task UpdateAsync(Tutor tutor, CancellationToken ct = default);

    Task DeleteAsync(Tutor tutor, CancellationToken ct = default);
}
