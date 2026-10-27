using VetClinic.Application.Common;
using VetClinic.Application.DTOs;
using VetClinic.Domain.Entities;

namespace VetClinic.Application.Interfaces;

public interface IPetRepository
{
    /// <summary>ERRO CUSTOMIZADO - RETORNO: devolve Failure(404) quando não existe.</summary>
    Task<OperationResult<Pet>> GetByIdAsync(int id, CancellationToken ct = default);

    Task<PageResult<PetResponse>> GetPagedAsync(int pageNumber, int pageSize, int? tutorId, CancellationToken ct = default);

    /// <summary>ERRO CUSTOMIZADO - LANÇAMENTO: lança NotFoundException se o TutorId não existir.</summary>
    Task AddAsync(Pet pet, CancellationToken ct = default);

    /// <summary>ERRO CUSTOMIZADO - LANÇAMENTO: lança NotFoundException se o TutorId não existir.</summary>
    Task UpdateAsync(Pet pet, CancellationToken ct = default);

    Task DeleteAsync(Pet pet, CancellationToken ct = default);
}
