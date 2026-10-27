using VetClinic.Application.Common;
using VetClinic.Application.DTOs;

namespace VetClinic.Application.Interfaces;

public interface ITutorService
{
    Task<OperationResult<PageResult<TutorResponse>>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default);
    Task<OperationResult<TutorResponse>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<OperationResult<TutorResponse>> CreateAsync(TutorRequest request, CancellationToken ct = default);
    Task<OperationResult<TutorResponse>> UpdateAsync(int id, TutorRequest request, CancellationToken ct = default);
    Task<OperationResult<bool>> DeleteAsync(int id, CancellationToken ct = default);
}

public interface IPetService
{
    Task<OperationResult<PageResult<PetResponse>>> GetPagedAsync(int pageNumber, int pageSize, int? tutorId, CancellationToken ct = default);
    Task<OperationResult<PetResponse>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<OperationResult<PetResponse>> CreateAsync(PetRequest request, CancellationToken ct = default);
    Task<OperationResult<PetResponse>> UpdateAsync(int id, PetRequest request, CancellationToken ct = default);
    Task<OperationResult<bool>> DeleteAsync(int id, CancellationToken ct = default);
}
