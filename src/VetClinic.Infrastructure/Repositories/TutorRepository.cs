using Microsoft.EntityFrameworkCore;
using VetClinic.Application.Common;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Errors;
using VetClinic.Infrastructure.Persistence;

namespace VetClinic.Infrastructure.Repositories;

public class TutorRepository : ITutorRepository
{
    private readonly VetClinicDbContext _context;

    public TutorRepository(VetClinicDbContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<Tutor>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        // OTIMIZACAO: AsNoTracking em leitura (Update/Delete reanexam a entidade).
        var tutor = await _context.Tutores.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

        // ERRO CUSTOMIZADO - RETORNO: falha devolvida como valor, sem exception.
        if (tutor is null)
            return OperationResult<Tutor>.Failure(
                new Error("Tutor.NaoEncontrado", $"Tutor com id {id} não foi encontrado."), 404);

        return OperationResult<Tutor>.Success(tutor);
    }

    public async Task<PageResult<TutorResponse>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        // OTIMIZACAO: AsNoTracking em leitura.
        var query = _context.Tutores.AsNoTracking();

        // OTIMIZACAO: contagem feita no banco (CountAsync).
        var total = await query.CountAsync(ct);

        // OTIMIZACAO: paginação no banco (Skip/Take) + projeção com Select direto pro DTO.
        var items = await query
            .OrderBy(t => t.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TutorResponse
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
            })
            .ToListAsync(ct);

        return new PageResult<TutorResponse>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task AddAsync(Tutor tutor, CancellationToken ct = default)
    {
        await EnsureEmailIsUniqueAsync(tutor.Email, tutor.Id, ct);
        _context.Tutores.Add(tutor);
        await SaveAsync(tutor.Email, ct);
    }

    public async Task UpdateAsync(Tutor tutor, CancellationToken ct = default)
    {
        await EnsureEmailIsUniqueAsync(tutor.Email, tutor.Id, ct);
        _context.Tutores.Update(tutor);
        await SaveAsync(tutor.Email, ct);
    }

    public async Task DeleteAsync(Tutor tutor, CancellationToken ct = default)
    {
        _context.Tutores.Remove(tutor);
        await _context.SaveChangesAsync(ct);
    }

    private async Task EnsureEmailIsUniqueAsync(string email, int currentId, CancellationToken ct)
    {
        var exists = await _context.Tutores.AsNoTracking()
            .AnyAsync(t => t.Email == email && t.Id != currentId, ct);

        // ERRO CUSTOMIZADO - LANÇAMENTO: exception capturada pelo ExceptionHandlingMiddleware (409).
        if (exists)
            throw new ConflictException("Tutor.EmailDuplicado", $"Já existe um tutor com o e-mail '{email}'.");
    }

    private async Task SaveAsync(string email, CancellationToken ct)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Tutores_Email") == true)
        {
            // ERRO CUSTOMIZADO - LANÇAMENTO: corrida entre duas requisições — o índice único garante.
            throw new ConflictException("Tutor.EmailDuplicado", $"Já existe um tutor com o e-mail '{email}'.");
        }
    }
}
