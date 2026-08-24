using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Catalog;
using GramShopPOS.Application.DTOs.Stores;
using GramShopPOS.Application.DTOs.Users;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GramShopPOS.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _users;
    public UsersController(IUserService users) => _users = users;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await _users.GetAsync(request, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _users.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _users.CreateAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken) =>
        Ok(await _users.UpdateAsync(id, request, cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/stores")]
public sealed class StoresController : ControllerBase
{
    private readonly IStoreService _stores;
    public StoresController(IStoreService stores) => _stores = stores;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await _stores.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _stores.GetByIdAsync(id, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStoreRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _stores.CreateAsync(request, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStoreRequest request, CancellationToken cancellationToken) =>
        Ok(await _stores.UpdateAsync(id, request, cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categories;
    public CategoriesController(ICategoryService categories) => _categories = categories;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await _categories.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _categories.GetByIdAsync(id, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _categories.CreateAsync(request, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken) =>
        Ok(await _categories.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _categories.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
