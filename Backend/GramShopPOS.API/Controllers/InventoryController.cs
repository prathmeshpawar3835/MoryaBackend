using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Inventory;
using GramShopPOS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GramShopPOS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/inventory")]
public sealed class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventory;
    public InventoryController(IInventoryService inventory) => _inventory = inventory;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] InventoryListRequest request, CancellationToken cancellationToken) =>
        Ok(await _inventory.GetAsync(request, cancellationToken));

    [HttpGet("{productId:int}")]
    public async Task<IActionResult> GetByProduct(int productId, [FromQuery] int storeId, CancellationToken cancellationToken) =>
        Ok(await _inventory.GetByProductAsync(productId, storeId, cancellationToken));

    [HttpGet("ledger")]
    public async Task<IActionResult> Ledger([FromQuery] InventoryListRequest request, [FromQuery] int? productId, CancellationToken cancellationToken) =>
        Ok(await _inventory.GetLedgerAsync(request, productId, cancellationToken));

    [HttpPost("stock-in")]
    public async Task<IActionResult> StockIn([FromBody] StockInRequest request, CancellationToken cancellationToken)
    {
        await _inventory.StockInAsync(request, cancellationToken);
        return Ok();
    }

    [HttpPost("adjust")]
    public async Task<IActionResult> Adjust([FromBody] StockAdjustRequest request, CancellationToken cancellationToken)
    {
        await _inventory.AdjustAsync(request, cancellationToken);
        return Ok();
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] StockTransferRequest request, CancellationToken cancellationToken)
    {
        await _inventory.TransferAsync(request, cancellationToken);
        return Ok();
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> LowStock([FromQuery] int? storeId, CancellationToken cancellationToken) =>
        Ok(await _inventory.GetLowStockAsync(storeId, cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/purchases")]
public sealed class PurchasesController : ControllerBase
{
    private readonly IPurchaseService _purchases;
    public PurchasesController(IPurchaseService purchases) => _purchases = purchases;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await _purchases.GetAsync(request, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _purchases.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _purchases.CreateAsync(request, cancellationToken));
}
