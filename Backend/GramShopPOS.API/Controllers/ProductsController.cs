using GramShopPOS.Application.DTOs.Catalog;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GramShopPOS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _products;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(IProductService products, IWebHostEnvironment environment)
    {
        _products = products;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] ProductListRequest request, CancellationToken cancellationToken) =>
        Ok(await _products.GetAsync(request, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, [FromQuery] int? storeId, CancellationToken cancellationToken) =>
        Ok(await _products.GetByIdAsync(id, storeId, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _products.CreateAsync(request, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken) =>
        Ok(await _products.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _products.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int? storeId, CancellationToken cancellationToken) =>
        Ok(await _products.SearchAsync(query ?? string.Empty, storeId, cancellationToken));

    [HttpGet("barcode/{barcode}")]
    public async Task<IActionResult> Barcode(string barcode, [FromQuery] int? storeId, CancellationToken cancellationToken) =>
        Ok(await _products.GetByBarcodeAsync(barcode, storeId, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("import/preview")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Preview(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return Ok(await _products.PreviewImportAsync(stream, file.FileName, cancellationToken));
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("import/confirm")]
    public async Task<IActionResult> Confirm([FromQuery] Guid batchId, CancellationToken cancellationToken) =>
        Ok(await _products.ConfirmImportAsync(batchId, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("import/template")]
    public async Task<IActionResult> Template(CancellationToken cancellationToken) =>
        Ok(await _products.GetImportTemplateAsync(cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:int}/image")]
    [RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> UploadImage(int id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("An image file is required.");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".svg"))
        {
            return BadRequest("Upload a JPG, PNG, WEBP, or SVG image.");
        }

        var folder = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", "products");
        Directory.CreateDirectory(folder);
        var fileName = $"{id}{ext}";
        var path = Path.Combine(folder, fileName);
        await using (var stream = System.IO.File.Create(path))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return Ok(await _products.SetImageAsync(id, $"/uploads/products/{fileName}", cancellationToken));
    }
}
