using VetClinic.Application.Common;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Errors;

namespace VetClinic.Application.Services;

public class PetService : IPetService
{
    private readonly IPetRepository _repository;

    public PetService(IPetRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<PageResult<PetResponse>>> GetPagedAsync(int pageNumber, int pageSize, int? tutorId, CancellationToken ct = default)
    {
        var (page, size) = PageResult<PetResponse>.Normalize(pageNumber, pageSize);
        var result = await _repository.GetPagedAsync(page, size, tutorId, ct);
        return OperationResult<PageResult<PetResponse>>.Success(result);
    }

    public async Task<OperationResult<PetResponse>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var found = await _repository.GetByIdAsync(id, ct);
        if (!found.IsSuccess)
            return OperationResult<PetResponse>.Failure(found.Error!, found.StatusCode);

        return OperationResult<PetResponse>.Success(ToResponse(found.Value!));
    }

    public async Task<OperationResult<PetResponse>> CreateAsync(PetRequest request, CancellationToken ct = default)
    {
        ValidateBirthDate(request.DataNascimento);

        var pet = new Pet
        {
            Nome = request.Nome.Trim(),
            Especie = request.Especie.Trim(),
            Raca = request.Raca?.Trim(),
            DataNascimento = request.DataNascimento.Date,
            TutorId = request.TutorId
        };

        // Pode lançar NotFoundException (TutorId inexistente) — tratada pelo middleware global.
        await _repository.AddAsync(pet, ct);

        return OperationResult<PetResponse>.Success(ToResponse(pet), 201);
    }

    public async Task<OperationResult<PetResponse>> UpdateAsync(int id, PetRequest request, CancellationToken ct = default)
    {
        var found = await _repository.GetByIdAsync(id, ct);
        if (!found.IsSuccess)
            return OperationResult<PetResponse>.Failure(found.Error!, found.StatusCode);

        ValidateBirthDate(request.DataNascimento);

        var pet = found.Value!;
        pet.Nome = request.Nome.Trim();
        pet.Especie = request.Especie.Trim();
        pet.Raca = request.Raca?.Trim();
        pet.DataNascimento = request.DataNascimento.Date;
        pet.TutorId = request.TutorId;

        await _repository.UpdateAsync(pet, ct);

        return OperationResult<PetResponse>.Success(ToResponse(pet));
    }

    public async Task<OperationResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
    {
        var found = await _repository.GetByIdAsync(id, ct);
        if (!found.IsSuccess)
            return OperationResult<bool>.Failure(found.Error!, found.StatusCode);

        await _repository.DeleteAsync(found.Value!, ct);
        return OperationResult<bool>.Success(true, 204);
    }

    private static void ValidateBirthDate(DateTime dataNascimento)
    {
        // ERRO CUSTOMIZADO - LANÇAMENTO: regra de negócio → 422 via middleware.
        if (dataNascimento.Date > DateTime.UtcNow.Date)
            throw new BusinessRuleException("Pet.DataNascimentoFutura", "A data de nascimento do pet não pode estar no futuro.");
    }

    private static PetResponse ToResponse(Pet p) => new()
    {
        Id = p.Id,
        Nome = p.Nome,
        Especie = p.Especie,
        Raca = p.Raca,
        DataNascimento = p.DataNascimento,
        TutorId = p.TutorId
    };
}
