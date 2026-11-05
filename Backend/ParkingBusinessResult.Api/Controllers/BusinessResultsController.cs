using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Common;
using ParkingBusinessResult.Application.DTOs;
using ParkingBusinessResult.Application.Features.BusinessResults.Commands.Create;
using ParkingBusinessResult.Application.Features.BusinessResults.Commands.Delete;
using ParkingBusinessResult.Application.Features.BusinessResults.Commands.Update;
using ParkingBusinessResult.Application.Features.BusinessResults.Queries.GetById;
using ParkingBusinessResult.Application.Features.BusinessResults.Queries.List;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Api.Controllers;

[Route("dev/business-results")]
[Authorize(Policy = "BusinessResultAccess")]
public sealed class BusinessResultsController : ControllerBase
{
    [HttpGet]
    public IActionResult List([FromServices] ListBusinessResultsHandler handler)
    {
        IReadOnlyList<ParkingBusinessResultRecord> results = handler.Handle(new ListBusinessResultsQuery(GetAccess()));
        return Ok(ApiResponse<IReadOnlyList<ParkingBusinessResultRecord>>.Success(results));
    }

    [HttpGet("{id:guid}")]
    public IActionResult Get(Guid id, [FromServices] GetBusinessResultHandler handler) =>
        Respond(handler.Handle(new GetBusinessResultQuery(id, GetAccess())));

    [HttpPost]
    public IActionResult Create([FromBody] CreateBusinessResultRequest request, [FromServices] CreateBusinessResultHandler handler) =>
        Respond(handler.Handle(new CreateBusinessResultCommand(request.ParkingLotId, request.ReportingPeriod,
            request.Revenue, request.CompletedVehicleCount, request.Note, GetAccess())));

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, [FromBody] UpdateBusinessResultRequest request, [FromServices] UpdateBusinessResultHandler handler) =>
        Respond(handler.Handle(new UpdateBusinessResultCommand(id, request.Revenue, request.CompletedVehicleCount,
            request.Note, GetAccess())));

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id, [FromServices] DeleteBusinessResultHandler handler) =>
        Respond(handler.Handle(new DeleteBusinessResultCommand(id, GetAccess())));

    private BusinessAccess GetAccess()
    {
        var parkingLotIds = new HashSet<Guid>();
        foreach (Claim claim in User.FindAll("parking_lot_id"))
        {
            if (Guid.TryParse(claim.Value, out Guid parkingLotId)) parkingLotIds.Add(parkingLotId);
        }
        return new BusinessAccess(User.IsInRole("ROLE_ADMIN"), parkingLotIds);
    }

    private IActionResult Respond(BusinessResultOutcome<ParkingBusinessResultRecord> outcome) =>
        outcome.Error is null
            ? Ok(ApiResponse<ParkingBusinessResultRecord>.Success(outcome.Data!))
            : Ok(ApiResponse<ParkingBusinessResultRecord>.BusinessError(outcome.Error));
}
