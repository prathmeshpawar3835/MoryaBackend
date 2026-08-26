using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Operations;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GramShopPOS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/discounts")]
public sealed class DiscountsController : ControllerBase
{
    private readonly IDiscountService _discounts;
    public DiscountsController(IDiscountService discounts) => _discounts = discounts;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? storeId, [FromQuery] bool activeOnly, [FromQuery] OfferCategory? category, CancellationToken cancellationToken) =>
        Ok(await _discounts.GetAsync(storeId, activeOnly, category, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StoreDiscountRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _discounts.CreateAsync(request, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] StoreDiscountRequest request, CancellationToken cancellationToken) =>
        Ok(await _discounts.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _discounts.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize]
[Route("api/suppliers")]
public sealed class SuppliersController : ControllerBase
{
    private readonly ISupplierService _suppliers;
    public SuppliersController(ISupplierService suppliers) => _suppliers = suppliers;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await _suppliers.GetAsync(request, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _suppliers.GetByIdAsync(id, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SupplierRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _suppliers.CreateAsync(request, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SupplierRequest request, CancellationToken cancellationToken) =>
        Ok(await _suppliers.UpdateAsync(id, request, cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/repairs")]
public sealed class RepairsController : ControllerBase
{
    private readonly IRepairService _repairs;
    public RepairsController(IRepairService repairs) => _repairs = repairs;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await _repairs.GetAsync(request, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _repairs.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRepairJobRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _repairs.CreateAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRepairJobRequest request, CancellationToken cancellationToken) =>
        Ok(await _repairs.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/payments")]
    public async Task<IActionResult> Pay(int id, [FromBody] CollectRepairPaymentRequest request, CancellationToken cancellationToken) =>
        Ok(await _repairs.CollectPaymentAsync(id, request, cancellationToken));

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> Pdf(int id, [FromServices] IPdfService pdf, CancellationToken cancellationToken) =>
        Ok(await pdf.RepairReceiptPdfAsync(id, cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/birthday")]
public sealed class BirthdayController : ControllerBase
{
    private readonly IBirthdayService _birthdays;
    public BirthdayController(IBirthdayService birthdays) => _birthdays = birthdays;

    [HttpGet("eligibility")]
    public async Task<IActionResult> Eligibility([FromQuery] int customerId, [FromQuery] int? storeId, CancellationToken cancellationToken) =>
        Ok(await _birthdays.GetEligibilityAsync(customerId, storeId, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("process-daily")]
    public async Task<IActionResult> ProcessDaily(CancellationToken cancellationToken) =>
        Ok(await _birthdays.ProcessDailyAsync(cancellationToken));
}
