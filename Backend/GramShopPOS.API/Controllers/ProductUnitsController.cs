using GramShopPOS.Application.DTOs.Catalog;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GramShopPOS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/product-units")]
public sealed class ProductUnitsController : ControllerBase
{
    private readonly IProductUnitService _units;
    private readonly ILabelDocumentService _labels;

    public ProductUnitsController(IProductUnitService units, ILabelDocumentService labels)
    {
        _units = units;
        _labels = labels;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] ProductUnitListRequest request, CancellationToken cancellationToken) =>
        Ok(await _units.GetAsync(request, cancellationToken));

    [HttpGet("lookup/{uniqueNumber}")]
    public async Task<IActionResult> Lookup(string uniqueNumber, [FromQuery] int? storeId, CancellationToken cancellationToken) =>
        Ok(await _units.LookupAsync(uniqueNumber, storeId, cancellationToken));

    [HttpGet("{id:int}/qr")]
    public async Task<IActionResult> Qr(int id, [FromQuery] int? productId, CancellationToken cancellationToken)
    {
        var data = await _units.GetLabelDataAsync(new ProductUnitIdsRequest { Ids = [id], ProductId = productId }, cancellationToken);
        var unit = data[0];
        return File(_labels.QrPng(unit.UniqueNumber), "image/png", $"{unit.UniqueNumber}.png");
    }

    [HttpGet("{id:int}/barcode")]
    public async Task<IActionResult> Barcode(int id, [FromQuery] int? productId, CancellationToken cancellationToken)
    {
        var data = await _units.GetLabelDataAsync(new ProductUnitIdsRequest { Ids = [id], ProductId = productId }, cancellationToken);
        var unit = data[0];
        return File(_labels.BarcodePng(unit.UniqueNumber), "image/svg+xml", $"{unit.UniqueNumber}-code128.svg");
    }

    [HttpPost("qr-zip")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> QrZip([FromBody] ProductUnitIdsRequest request, CancellationToken cancellationToken)
    {
        var data = await _units.GetLabelDataAsync(request, cancellationToken);
        return Ok(_labels.QrZip(data));
    }

    [HttpPost("tags-pdf")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> TagsPdf([FromBody] ProductUnitIdsRequest request, CancellationToken cancellationToken)
    {
        var data = await _units.GetLabelDataAsync(request, cancellationToken);
        var width = request.WidthMm > 0 ? request.WidthMm : 50;
        var height = request.HeightMm > 0 ? request.HeightMm : 30;
        return Ok(_labels.TagsPdf(data, width, height));
    }
}
