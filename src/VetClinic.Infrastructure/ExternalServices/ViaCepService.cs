using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using VetClinic.Application.Common;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;

namespace VetClinic.Infrastructure.ExternalServices;

/// <summary>
/// Cliente tipado do ViaCEP. A resiliência (retry, circuit breaker, timeouts) vem do
/// AddStandardResilienceHandler() registrado no DependencyInjection; aqui o try/catch
/// garante que falhas viram OperationResult (400/404/503) sem derrubar a API.
/// </summary>
public class ViaCepService : ICepService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ViaCepService> _logger;

    public ViaCepService(HttpClient httpClient, ILogger<ViaCepService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OperationResult<EnderecoDto>> GetAddressAsync(string cep, CancellationToken ct = default)
    {
        var digits = new string((cep ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length != 8)
            return OperationResult<EnderecoDto>.Failure(
                new Error("Cep.Invalido", "CEP inválido. Informe 8 dígitos (ex.: 01001-000)."), 400);

        try
        {
            using var response = await _httpClient.GetAsync($"{digits}/json/", ct);

            if (response.StatusCode == HttpStatusCode.BadRequest)
                return OperationResult<EnderecoDto>.Failure(
                    new Error("Cep.Invalido", "CEP rejeitado pelo ViaCEP."), 400);

            if (!response.IsSuccessStatusCode)
                return Unavailable($"ViaCEP respondeu {(int)response.StatusCode}.");

            var body = await response.Content.ReadFromJsonAsync<ViaCepResponse>(cancellationToken: ct);
            if (body is null || body.Erro is not null)
                return OperationResult<EnderecoDto>.Failure(
                    new Error("Cep.NaoEncontrado", $"CEP {digits} não encontrado."), 404);

            var formatted = $"{digits[..5]}-{digits[5..]}";
            return OperationResult<EnderecoDto>.Success(
                new EnderecoDto(formatted, body.Logradouro, body.Bairro, body.Localidade, body.Uf));
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            // Timeout, circuito aberto, DNS, rede... → 503 em vez de 500.
            _logger.LogWarning(ex, "Falha ao consultar ViaCEP para o CEP {Cep}", digits);
            return Unavailable("Serviço de CEP (ViaCEP) indisponível no momento. Tente novamente.");
        }
    }

    private static OperationResult<EnderecoDto> Unavailable(string message) =>
        OperationResult<EnderecoDto>.Failure(new Error("Cep.ServicoIndisponivel", message), 503);

    private sealed class ViaCepResponse
    {
        [JsonPropertyName("cep")] public string? Cep { get; set; }
        [JsonPropertyName("logradouro")] public string? Logradouro { get; set; }
        [JsonPropertyName("bairro")] public string? Bairro { get; set; }
        [JsonPropertyName("localidade")] public string? Localidade { get; set; }
        [JsonPropertyName("uf")] public string? Uf { get; set; }

        // ViaCEP devolve { "erro": true } (ou "true") quando o CEP não existe.
        [JsonPropertyName("erro")] public object? Erro { get; set; }
    }
}
