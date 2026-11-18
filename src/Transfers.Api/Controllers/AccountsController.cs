using Microsoft.AspNetCore.Mvc;
using Transfers.Application.Accounts;

namespace Transfers.Api.Controllers
{
    [ApiController]
    [Route("api/accounts")]
    public class AccountsController(AccountService accountService) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<IReadOnlyList<AccountResponse>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<AccountResponse>>> List(CancellationToken ct) =>
            Ok(await accountService.ListAsync(ct));

        [HttpGet("{id:guid}")]
        [ProducesResponseType<AccountResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AccountResponse>> GetById(Guid id, CancellationToken ct) =>
            await accountService.GetByIdAsync(id, ct);
    }
}
