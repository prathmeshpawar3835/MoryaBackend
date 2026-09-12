using GramShopPOS.Application.DTOs.Ecommerce;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GramShopPOS.API.Controllers;

[ApiController]
[Route("api/ecommerce")]
public sealed class EcommerceController : ControllerBase
{
    private readonly IEcommerceService _shop;
    public EcommerceController(IEcommerceService shop) => _shop = shop;

    [HttpGet("shop")]
    [AllowAnonymous]
    public async Task<IActionResult> Shop(CancellationToken cancellationToken) =>
        Ok(await _shop.GetShopInfoAsync(cancellationToken));

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> Categories(CancellationToken cancellationToken) =>
        Ok(await _shop.GetCategoriesAsync(cancellationToken));

    [HttpGet("catalog")]
    [AllowAnonymous]
    public async Task<IActionResult> Catalog([FromQuery] ShopCatalogRequest request, CancellationToken cancellationToken) =>
        Ok(await _shop.GetCatalogAsync(request, cancellationToken));

    [HttpGet("products/{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Product(int id, CancellationToken cancellationToken) =>
        Ok(await _shop.GetProductAsync(id, cancellationToken));

    [HttpGet("offers")]
    [AllowAnonymous]
    public async Task<IActionResult> Offers(CancellationToken cancellationToken) =>
        Ok(await _shop.GetOffersAsync(cancellationToken));

    [HttpPost("auth/register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] ShopRegisterRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _shop.RegisterAsync(request, cancellationToken));

    [HttpPost("auth/login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] ShopLoginRequest request, CancellationToken cancellationToken) =>
        Ok(await _shop.LoginAsync(request, cancellationToken));

    [HttpGet("me")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken) =>
        Ok(await _shop.GetMeAsync(cancellationToken));

    [HttpGet("orders")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Orders([FromQuery] Application.Common.PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await _shop.GetOrdersAsync(request, cancellationToken));

    [HttpGet("orders/{id:int}")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Order(int id, CancellationToken cancellationToken) =>
        Ok(await _shop.GetOrderAsync(id, cancellationToken));

    [HttpPost("checkout")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Checkout([FromBody] ShopCheckoutRequest request, CancellationToken cancellationToken) =>
        Ok(await _shop.CheckoutAsync(request, cancellationToken));
}
