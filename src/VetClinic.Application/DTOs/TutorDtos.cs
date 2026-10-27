using System.ComponentModel.DataAnnotations;

namespace VetClinic.Application.DTOs;

public class TutorRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [StringLength(100, MinimumLength = 2)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "CEP é obrigatório.")]
    [RegularExpression(@"^\d{5}-?\d{3}$", ErrorMessage = "CEP deve ter o formato 00000-000 ou 00000000.")]
    public string Cep { get; set; } = string.Empty;
}

public class TutorResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Cep { get; set; } = string.Empty;
    public string? Logradouro { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public DateTime CriadoEm { get; set; }
    public List<LinkDto> Links { get; set; } = new();
}
