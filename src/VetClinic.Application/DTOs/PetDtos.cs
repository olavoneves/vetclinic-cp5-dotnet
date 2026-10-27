using System.ComponentModel.DataAnnotations;

namespace VetClinic.Application.DTOs;

public class PetRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [StringLength(100, MinimumLength = 1)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Espécie é obrigatória.")]
    [StringLength(50)]
    public string Especie { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Raca { get; set; }

    [Required]
    public DateTime DataNascimento { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "TutorId inválido.")]
    public int TutorId { get; set; }
}

public class PetResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string? Raca { get; set; }
    public DateTime DataNascimento { get; set; }
    public int TutorId { get; set; }
    public List<LinkDto> Links { get; set; } = new();
}
