using Microsoft.AspNetCore.Mvc;
using Transfers.Application.Transfers;
using Transfers.Domain.Transfers;

namespace Transfers.Api.Controllers
{
    [ApiController]
    [Route("api/transfers")]
    public class TransfersController(TransferService transferService) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType<TransferResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Create(CreateTransferRequest request, CancellationToken ct)
        {
            var transfer = await transferService.CreateAsync(request, ct);

            if (transfer.Status == nameof(TransferStatus.Failed))
            {
                return Rejected(transfer);
            }

            return CreatedAtAction(nameof(GetById), new { id = transfer.Id }, transfer);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TransferResponse>> GetById(Guid id, CancellationToken ct) =>
            await transferService.GetByIdAsync(id, ct);

        private UnprocessableEntityObjectResult Rejected(TransferResponse transfer)
        {
            var problem = ProblemDetailsFactory.CreateProblemDetails(
                HttpContext,
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Transfer rejected",
                detail: $"The transfer was recorded as Failed: {transfer.FailureReason}.",
                instance: Url.Action(nameof(GetById), new { id = transfer.Id }));

            problem.Extensions["transferId"] = transfer.Id;
            problem.Extensions["reason"] = transfer.FailureReason;

            return UnprocessableEntity(problem);
        }
    }
}
