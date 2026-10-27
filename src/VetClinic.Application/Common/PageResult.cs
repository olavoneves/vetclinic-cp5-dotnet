using VetClinic.Application.DTOs;

namespace VetClinic.Application.Common;

public class PageResult<T>
{
    public const int MaxPageSize = 50;

    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;

    // HATEOAS da página (self, first, last, next, previous) — preenchido na API.
    public List<LinkDto> Links { get; set; } = new();

    /// <summary>Normaliza parâmetros de paginação (pageSize máximo 50).</summary>
    public static (int PageNumber, int PageSize) Normalize(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;
        return (pageNumber, pageSize);
    }
}
