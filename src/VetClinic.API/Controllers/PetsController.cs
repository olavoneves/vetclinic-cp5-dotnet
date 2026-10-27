using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.API.Hateoas;
using VetClinic.Application.Common;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;

namespace VetClinic.API.Controllers;

[Route("api/pets")]
[Authorize]
public class PetsController : BaseApiController
{
    private readonly IPetService _service;

    public PetsController(IPetService service)
    {
        _service = service;
    }

    /// <summary>Lista pets paginados; filtro opcional por tutorId.</summary>
    [HttpGet(Name = RouteNames.GetPets)]
    [ProducesResponseType(typeof(PageResult<PetResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10,
        [FromQuery] int? tutorId = null, CancellationToken ct = default)
    {
        var result = await _service.GetPagedAsync(pageNumber, pageSize, tutorId, ct);
        return ToActionResult(result, page =>
        {
            foreach (var pet in page.Items) pet.WithLinks(Url);
            object? filter = tutorId.HasValue ? new { tutorId } : null;
            return Ok(page.WithPageLinks(Url, RouteNames.GetPets, filter));
        });
    }

    [HttpGet("{id:int}", Name = RouteNames.GetPetById)]
    [ProducesResponseType(typeof(PetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return ToActionResult(result, pet => Ok(pet.WithLinks(Url)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] PetRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return ToActionResult(result, pet =>
            CreatedAtRoute(RouteNames.GetPetById, new { id = pet.Id }, pet.WithLinks(Url)));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] PetRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, request, ct);
        return ToActionResult(result, pet => Ok(pet.WithLinks(Url)));
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
