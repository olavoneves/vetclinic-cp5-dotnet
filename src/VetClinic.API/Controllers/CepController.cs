using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;

namespace VetClinic.API.Controllers;

[Route("api/cep")]
[AllowAnonymous]
public class CepController : BaseApiController
{
    private readonly ICepService _cepService;

    public CepController(ICepService cepService)
    {
        _cepService = cepService;
    }

    /// <summary>Consulta isolada ao ViaCEP (demonstra o serviço externo e a resiliência).</summary>
    [HttpGet("{cep}")]
    // OTIMIZACAO: OutputCache — CEP não muda; a mesma consulta é servida da memória por 5 minutos.
    [OutputCache(Duration = 300)]
    [ProducesResponseType(typeof(EnderecoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(string cep, CancellationToken ct)
    {
        var result = await _cepService.GetAddressAsync(cep, ct);
        return ToActionResult(result);
    }
}
