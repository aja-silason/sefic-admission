using Microsoft.AspNetCore.Mvc;
using Partners.Api.Contracts;
using Partners.Application.Partners;

namespace Partners.Api.Controllers;

[ApiController]
[Route("api/partners")]
public sealed class PartnersController : ControllerBase
{
    /// <summary>Creates a partner (customer, supplier or both).</summary>
    [HttpPost]
    [ProducesResponseType<PartnerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PartnerResponse>> Create(
        [FromBody] CreatePartnerRequest request,
        [FromServices] CreatePartnerUseCase useCase,
        CancellationToken cancellationToken)
    {
        var partner = await useCase.ExecuteAsync(request.ToInput(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = partner.Id }, partner);
    }

    /// <summary>Gets a partner by Id, including inactive ones.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType<PartnerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<PartnerResponse> GetById(
        Guid id,
        [FromServices] GetPartnerUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(id, cancellationToken);

    /// <summary>Lists partners with optional search, type filter and paging (ordered by Name, then Id).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<PartnerResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PagedResponse<PartnerResponse>> List(
        [FromQuery] ListPartnersRequest request,
        [FromServices] ListPartnersUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(request.ToInput(), cancellationToken);

    /// <summary>Replaces a partner's data (also used to deactivate/reactivate). Requires the current Version.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType<PartnerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<PartnerResponse> Update(
        Guid id,
        [FromBody] UpdatePartnerRequest request,
        [FromServices] UpdatePartnerUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(id, request.ToInput(), cancellationToken);
}
