namespace VetClinic.Application.DTOs;

public record EnderecoDto(string Cep, string? Logradouro, string? Bairro, string? Cidade, string? Uf);
