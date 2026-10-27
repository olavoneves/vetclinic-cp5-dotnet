using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.API.Hateoas;
using VetClinic.Application.Common;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;

namespace VetClinic.API.Controllers;

[Route("api/tutores")]
[Authorize]
public class TutoresController : BaseApiController
{
    private readonly ITutorService _service;

    public TutoresController(ITutorService service)
    {
        _service = service;
    }

    /// <summary>Lista tutores paginados (pageSize máximo 50).</summary>
    [HttpGet(Name = RouteNames.GetTutores)]
    [ProducesResponseType(typeof(PageResult<TutorResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _service.GetPagedAsync(pageNumber, pageSize, ct);
        return ToActionResult(result, page =>
        {
            foreach (var tutor in page.Items) tutor.WithLinks(Url);
            return Ok(page.WithPageLinks(Url, RouteNames.GetTutores));
        });
    }

    [HttpGet("{id:int}", Name = RouteNames.GetTutorById)]
    [ProducesResponseType(typeof(TutorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return ToActionResult(result, tutor => Ok(tutor.WithLinks(Url)));
    }

    /// <summary>Cria um tutor; o endereço é preenchido pelo ViaCEP a partir do CEP.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TutorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create([FromBody] TutorRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return ToActionResult(result, tutor =>
            CreatedAtRoute(RouteNames.GetTutorById, new { id = tutor.Id }, tutor.WithLinks(Url)));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TutorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] TutorRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, request, ct);
        return ToActionResult(result, tutor => Ok(tutor.WithLinks(Url)));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        return ToActionResult(result);
    }
}
