using VetClinic.Application.Common;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;

namespace VetClinic.Application.Services;

public class TutorService : ITutorService
{
    private readonly ITutorRepository _repository;
    private readonly ICepService _cepService;

    public TutorService(ITutorRepository repository, ICepService cepService)
    {
        _repository = repository;
        _cepService = cepService;
    }

    public async Task<OperationResult<PageResult<TutorResponse>>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var (page, size) = PageResult<TutorResponse>.Normalize(pageNumber, pageSize);
        var result = await _repository.GetPagedAsync(page, size, ct);
        return OperationResult<PageResult<TutorResponse>>.Success(result);
    }

    public async Task<OperationResult<TutorResponse>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var found = await _repository.GetByIdAsync(id, ct);
        if (!found.IsSuccess)
            return OperationResult<TutorResponse>.Failure(found.Error!, found.StatusCode);

        return OperationResult<TutorResponse>.Success(ToResponse(found.Value!));
    }

    public async Task<OperationResult<TutorResponse>> CreateAsync(TutorRequest request, CancellationToken ct = default)
    {
        // Serviço externo: endereço preenchido a partir do CEP (ViaCEP).
        var endereco = await _cepService.GetAddressAsync(request.Cep, ct);
        if (!endereco.IsSuccess)
            return OperationResult<TutorResponse>.Failure(endereco.Error!, endereco.StatusCode);

        var tutor = new Tutor
        {
            Nome = request.Nome.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            CriadoEm = DateTime.UtcNow
        };
        ApplyAddress(tutor, endereco.Value!);

        // Pode lançar ConflictException (e-mail duplicado) — tratada pelo middleware global.
        await _repository.AddAsync(tutor, ct);

        return OperationResult<TutorResponse>.Success(ToResponse(tutor), 201);
    }

    public async Task<OperationResult<TutorResponse>> UpdateAsync(int id, TutorRequest request, CancellationToken ct = default)
    {
        var found = await _repository.GetByIdAsync(id, ct);
        if (!found.IsSuccess)
            return OperationResult<TutorResponse>.Failure(found.Error!, found.StatusCode);

        var tutor = found.Value!;
        var endereco = await _cepService.GetAddressAsync(request.Cep, ct);
        if (!endereco.IsSuccess)
            return OperationResult<TutorResponse>.Failure(endereco.Error!, endereco.StatusCode);

        tutor.Nome = request.Nome.Trim();
        tutor.Email = request.Email.Trim().ToLowerInvariant();
        ApplyAddress(tutor, endereco.Value!);

        await _repository.UpdateAsync(tutor, ct);

        return OperationResult<TutorResponse>.Success(ToResponse(tutor));
    }

    public async Task<OperationResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
    {
        var found = await _repository.GetByIdAsync(id, ct);
        if (!found.IsSuccess)
            return OperationResult<bool>.Failure(found.Error!, found.StatusCode);

        await _repository.DeleteAsync(found.Value!, ct);
        return OperationResult<bool>.Success(true, 204);
    }

    private static void ApplyAddress(Tutor tutor, EnderecoDto endereco)
    {
        tutor.Cep = endereco.Cep;
        tutor.Logradouro = endereco.Logradouro;
        tutor.Bairro = endereco.Bairro;
        tutor.Cidade = endereco.Cidade;
        tutor.Uf = endereco.Uf;
    }

    private static TutorResponse ToResponse(Tutor t) => new()
    {
        Id = t.Id,
        Nome = t.Nome,
        Email = t.Email,
        Cep = t.Cep,
        Logradouro = t.Logradouro,
        Bairro = t.Bairro,
        Cidade = t.Cidade,
        Uf = t.Uf,
        CriadoEm = t.CriadoEm
    };
}
