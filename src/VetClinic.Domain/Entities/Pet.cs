namespace VetClinic.Domain.Entities;

public class Pet
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string? Raca { get; set; }
    public DateTime DataNascimento { get; set; }

    public int TutorId { get; set; }
    public Tutor? Tutor { get; set; }
}
