using Microsoft.EntityFrameworkCore;
using VetClinic.Application.Common;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Errors;
using VetClinic.Infrastructure.Persistence;

namespace VetClinic.Infrastructure.Repositories;

public class PetRepository : IPetRepository
{
    private readonly VetClinicDbContext _context;

    public PetRepository(VetClinicDbContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<Pet>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        // OTIMIZACAO: AsNoTracking em leitura.
        var pet = await _context.Pets.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

        // ERRO CUSTOMIZADO - RETORNO: falha devolvida como valor, sem exception.
        if (pet is null)
            return OperationResult<Pet>.Failure(
                new Error("Pet.NaoEncontrado", $"Pet com id {id} não foi encontrado."), 404);

        return OperationResult<Pet>.Success(pet);
    }

    public async Task<PageResult<PetResponse>> GetPagedAsync(int pageNumber, int pageSize, int? tutorId, CancellationToken ct = default)
    {
        // OTIMIZACAO: AsNoTracking em leitura (+ filtro usa o índice IX_Pets_TutorId).
        var query = _context.Pets.AsNoTracking();
        if (tutorId.HasValue)
            query = query.Where(p => p.TutorId == tutorId.Value);

        // OTIMIZACAO: contagem feita no banco (CountAsync).
        var total = await query.CountAsync(ct);

        // OTIMIZACAO: paginação no banco (Skip/Take) + projeção com Select direto pro DTO.
        var items = await query
            .OrderBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PetResponse
            {
                Id = p.Id,
                Nome = p.Nome,
                Especie = p.Especie,
                Raca = p.Raca,
                DataNascimento = p.DataNascimento,
                TutorId = p.TutorId
            })
            .ToListAsync(ct);

        return new PageResult<PetResponse>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task AddAsync(Pet pet, CancellationToken ct = default)
    {
        await EnsureTutorExistsAsync(pet.TutorId, ct);
        _context.Pets.Add(pet);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Pet pet, CancellationToken ct = default)
    {
        await EnsureTutorExistsAsync(pet.TutorId, ct);
        _context.Pets.Update(pet);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Pet pet, CancellationToken ct = default)
    {
        _context.Pets.Remove(pet);
        await _context.SaveChangesAsync(ct);
    }

    private async Task EnsureTutorExistsAsync(int tutorId, CancellationToken ct)
    {
        var exists = await _context.Tutores.AsNoTracking().AnyAsync(t => t.Id == tutorId, ct);

        // ERRO CUSTOMIZADO - LANÇAMENTO: exception capturada pelo ExceptionHandlingMiddleware (404).
        if (!exists)
            throw new NotFoundException("Tutor.NaoEncontrado", $"Tutor com id {tutorId} não existe; não é possível vincular o pet.");
    }
}
