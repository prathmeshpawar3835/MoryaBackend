using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GramShopPOS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/pos")]
public sealed class PosController : ControllerBase
{
    private readonly IBillingService _billing;
    private readonly IUserService _users;
    public PosController(IBillingService billing, IUserService users)
    {
        _billing = billing;
        _users = users;
    }

    [HttpGet("sales-persons")]
    public async Task<IActionResult> SalesPersons([FromQuery] int storeId, CancellationToken cancellationToken) =>
        Ok(await _users.GetSalesPersonsAsync(storeId, cancellationToken));

    [HttpPost("bills")]
    public async Task<IActionResult> Create([FromBody] CreateBillRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _billing.CreateBillAsync(request, cancellationToken));

    [HttpPost("held-bills")]
    public async Task<IActionResult> Hold([FromBody] HeldBillRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _billing.HoldBillAsync(request, cancellationToken));

    [HttpGet("held-bills")]
    public async Task<IActionResult> Held([FromQuery] int? storeId, CancellationToken cancellationToken) =>
        Ok(await _billing.GetHeldBillsAsync(storeId, cancellationToken));

    [HttpGet("held-bills/{id:int}")]
    public async Task<IActionResult> HeldById(int id, CancellationToken cancellationToken) =>
        Ok(await _billing.GetHeldBillAsync(id, cancellationToken));

    [HttpPost("held-bills/{id:int}/resume")]
    public async Task<IActionResult> Resume(int id, CancellationToken cancellationToken) =>
        Ok(await _billing.ResumeHeldBillAsync(id, cancellationToken));

    [HttpDelete("held-bills/{id:int}")]
    public async Task<IActionResult> DeleteHeld(int id, CancellationToken cancellationToken)
    {
        await _billing.DeleteHeldBillAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize]
[Route("api/bills")]
public sealed class BillsController : ControllerBase
{
    private readonly IBillingService _billing;
    private readonly IPdfService _pdf;
    public BillsController(IBillingService billing, IPdfService pdf)
    {
        _billing = billing;
        _pdf = pdf;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] BillListRequest request, CancellationToken cancellationToken) =>
        Ok(await _billing.GetBillsAsync(request, cancellationToken));

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] BillListRequest request, CancellationToken cancellationToken) =>
        Ok(await _billing.SearchBillsAsync(request, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _billing.GetBillAsync(id, cancellationToken));

    [HttpGet("{id:int}/invoice")]
    public async Task<IActionResult> Invoice(int id, CancellationToken cancellationToken) =>
        Ok(await _billing.GetInvoiceAsync(id, cancellationToken));

    [HttpGet("{id:int}/invoice/pdf")]
    public async Task<IActionResult> InvoicePdf(int id, CancellationToken cancellationToken) =>
        Ok(await _pdf.InvoicePdfAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelRequest? request, CancellationToken cancellationToken)
    {
        await _billing.CancelBillAsync(id, request?.Reason, cancellationToken);
        return Ok();
    }
}

public sealed class CancelRequest
{
    public string? Reason { get; set; }
}

[ApiController]
[Authorize]
[Route("api/returns")]
public sealed class ReturnsController : ControllerBase
{
    private readonly IReturnService _returns;
    private readonly IPdfService _pdf;
    public ReturnsController(IReturnService returns, IPdfService pdf)
    {
        _returns = returns;
        _pdf = pdf;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReturnRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _returns.CreateReturnAsync(request, cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Application.Common.PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await _returns.GetAsync(request, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _returns.GetByIdAsync(id, cancellationToken));

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> Pdf(int id, CancellationToken cancellationToken) =>
        Ok(await _pdf.ReturnNotePdfAsync(id, cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/exchanges")]
public sealed class ExchangesController : ControllerBase
{
    private readonly IReturnService _returns;
    public ExchangesController(IReturnService returns) => _returns = returns;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExchangeRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _returns.CreateExchangeAsync(request, cancellationToken));
}
