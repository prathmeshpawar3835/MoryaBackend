using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Catalog;
using GramShopPOS.Application.DTOs.Stores;
using GramShopPOS.Application.DTOs.Users;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordService _passwords;
    private readonly IAuditService _audit;

    public UserService(IAppDbContext db, ICurrentUser currentUser, IPasswordService passwords, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _passwords = passwords;
        _audit = audit;
    }

    public async Task<PagedResponse<UserDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        var query = _db.Users.AsNoTracking().Where(u => !u.IsDeleted);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(u => u.UserName.Contains(s) || u.FullName.Contains(s));
        }

        var total = await query.CountAsync(cancellationToken);
        var users = await query.OrderBy(u => u.UserName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.UserRoles.Select(r => r.Role.Name).FirstOrDefault() ?? string.Empty,
                IsActive = u.IsActive,
                MustChangePassword = u.MustChangePassword,
                StoreIds = u.StoreUsers.Select(s => s.StoreId).ToList(),
                CreatedDate = u.CreatedDate
            })
            .ToListAsync(cancellationToken);

        return PagedResponse<UserDto>.Create(users, request.PageNumber, request.PageSize, total);
    }

    public async Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        return await MapAsync(id, cancellationToken);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        _passwords.ValidateStrength(request.Password);
        if (!Roles.All.Contains(request.Role))
        {
            throw new ValidationAppException("Invalid role.");
        }

        if (await _db.Users.AnyAsync(u => u.UserName == request.UserName && !u.IsDeleted, cancellationToken))
        {
            throw new ConflictAppException("Username already exists.");
        }

        var role = await _db.Roles.FirstAsync(r => r.Name == request.Role, cancellationToken);
        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            FullName = request.FullName.Trim(),
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = _passwords.Hash(request.Password),
            MustChangePassword = true,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await AssignStoresAsync(user.Id, request.StoreIds, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.UserCreated, nameof(ApplicationUser), user.Id.ToString(), null, new { user.UserName, request.Role }, null, cancellationToken);
        return await MapAsync(user.Id, cancellationToken);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        var user = await _db.Users.Include(u => u.UserRoles).Include(u => u.StoreUsers)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("User not found.");

        if (!Roles.All.Contains(request.Role))
        {
            throw new ValidationAppException("Invalid role.");
        }

        user.FullName = request.FullName.Trim();
        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;
        user.IsActive = request.IsActive;
        user.UpdatedDate = DateTime.UtcNow;
        user.UpdatedBy = _currentUser.UserId;
        if (!user.IsActive)
        {
            user.IsDeleted = false;
        }

        var role = await _db.Roles.FirstAsync(r => r.Name == request.Role, cancellationToken);
        _db.UserRoles.RemoveRange(user.UserRoles);
        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        _db.StoreUsers.RemoveRange(user.StoreUsers);
        await AssignStoresAsync(user.Id, request.StoreIds, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(request.IsActive ? AuditActions.UserUpdated : AuditActions.UserDeactivated, nameof(ApplicationUser), user.Id.ToString(), null, new { user.IsActive, request.Role }, null, cancellationToken);
        return await MapAsync(user.Id, cancellationToken);
    }

    private async Task AssignStoresAsync(int userId, IReadOnlyList<int> storeIds, CancellationToken cancellationToken)
    {
        var unique = storeIds.Distinct().ToList();
        var count = await _db.Stores.CountAsync(s => unique.Contains(s.Id) && !s.IsDeleted, cancellationToken);
        if (count != unique.Count)
        {
            throw new ValidationAppException("One or more stores are invalid.");
        }

        var first = true;
        foreach (var storeId in unique)
        {
            _db.StoreUsers.Add(new StoreUser
            {
                UserId = userId,
                StoreId = storeId,
                IsPrimary = first,
                CreatedDate = DateTime.UtcNow
            });
            first = false;
        }
    }

    private async Task<UserDto> MapAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.UserRoles.Select(r => r.Role.Name).FirstOrDefault() ?? string.Empty,
                IsActive = u.IsActive,
                MustChangePassword = u.MustChangePassword,
                StoreIds = u.StoreUsers.Select(s => s.StoreId).ToList(),
                CreatedDate = u.CreatedDate
            })
            .FirstAsync(cancellationToken);
    }
}

public sealed class StoreService : IStoreService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public StoreService(IAppDbContext db, ICurrentUser currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IReadOnlyList<StoreDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var query = _db.Stores.AsNoTracking().Where(s => !s.IsDeleted);
        if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            query = query.Where(s => ids.Contains(s.Id));
        }

        return await query.OrderBy(s => s.StoreName).Select(s => new StoreDto
        {
            Id = s.Id,
            StoreCode = s.StoreCode,
            StoreName = s.StoreName,
            Address = s.Address,
            ContactNumber = s.ContactNumber,
            GSTNumber = s.GSTNumber,
            InvoicePrefix = s.InvoicePrefix,
            IsActive = s.IsActive,
            CreatedDate = s.CreatedDate,
            UpdatedDate = s.UpdatedDate
        }).ToListAsync(cancellationToken);
    }

    public async Task<StoreDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _currentUser.Access().EnsureStoreAccess(id);
        var store = await _db.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Store not found.");
        return Map(store);
    }

    public async Task<StoreDto> CreateAsync(CreateStoreRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        if (await _db.Stores.AnyAsync(s => s.StoreCode == request.StoreCode && !s.IsDeleted, cancellationToken))
        {
            throw new ConflictAppException("Store code already exists.");
        }

        var store = new Store
        {
            StoreCode = request.StoreCode.Trim().ToUpperInvariant(),
            StoreName = request.StoreName.Trim(),
            Address = request.Address,
            ContactNumber = request.ContactNumber,
            GSTNumber = request.GSTNumber,
            InvoicePrefix = string.IsNullOrWhiteSpace(request.InvoicePrefix) ? request.StoreCode.Trim().ToUpperInvariant() : request.InvoicePrefix.Trim().ToUpperInvariant(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };
        _db.Stores.Add(store);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.StoreCreated, nameof(Store), store.Id.ToString(), null, store, store.Id, cancellationToken);
        return Map(store);
    }

    public async Task<StoreDto> UpdateAsync(int id, UpdateStoreRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Store not found.");

        if (await _db.Stores.AnyAsync(s => s.StoreCode == request.StoreCode && s.Id != id && !s.IsDeleted, cancellationToken))
        {
            throw new ConflictAppException("Store code already exists.");
        }

        store.StoreCode = request.StoreCode.Trim().ToUpperInvariant();
        store.StoreName = request.StoreName.Trim();
        store.Address = request.Address;
        store.ContactNumber = request.ContactNumber;
        store.GSTNumber = request.GSTNumber;
        store.InvoicePrefix = string.IsNullOrWhiteSpace(request.InvoicePrefix) ? store.StoreCode : request.InvoicePrefix.Trim().ToUpperInvariant();
        store.IsActive = request.IsActive;
        store.UpdatedDate = DateTime.UtcNow;
        store.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.StoreUpdated, nameof(Store), store.Id.ToString(), null, store, store.Id, cancellationToken);
        return Map(store);
    }

    private static StoreDto Map(Store s) => new()
    {
        Id = s.Id,
        StoreCode = s.StoreCode,
        StoreName = s.StoreName,
        Address = s.Address,
        ContactNumber = s.ContactNumber,
        GSTNumber = s.GSTNumber,
        InvoicePrefix = s.InvoicePrefix,
        IsActive = s.IsActive,
        CreatedDate = s.CreatedDate,
        UpdatedDate = s.UpdatedDate
    };
}

public sealed class CategoryService : ICategoryService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public CategoryService(IAppDbContext db, ICurrentUser currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        return await _db.Categories.AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description, IsActive = c.IsActive, CreatedDate = c.CreatedDate })
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var c = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Category not found.");
        return new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description, IsActive = c.IsActive, CreatedDate = c.CreatedDate };
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        if (await _db.Categories.AnyAsync(c => c.Name == request.Name && !c.IsDeleted, cancellationToken))
        {
            throw new ConflictAppException("Category name already exists.");
        }

        var category = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.CategoryCreated, nameof(Category), category.Id.ToString(), null, category, null, cancellationToken);
        return new CategoryDto { Id = category.Id, Name = category.Name, Description = category.Description, IsActive = true, CreatedDate = category.CreatedDate };
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Category not found.");
        category.Name = request.Name.Trim();
        category.Description = request.Description;
        category.IsActive = request.IsActive;
        category.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.CategoryUpdated, nameof(Category), category.Id.ToString(), null, category, null, cancellationToken);
        return new CategoryDto { Id = category.Id, Name = category.Name, Description = category.Description, IsActive = category.IsActive, CreatedDate = category.CreatedDate };
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Category not found.");

        if (await _db.Products.AnyAsync(p => p.CategoryId == id && !p.IsDeleted, cancellationToken) ||
            await _db.BillItems.AnyAsync(b => b.Product.CategoryId == id, cancellationToken))
        {
            category.IsDeleted = true;
            category.IsActive = false;
        }
        else
        {
            category.IsDeleted = true;
            category.IsActive = false;
        }

        category.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.CategoryDeleted, nameof(Category), id.ToString(), null, null, null, cancellationToken);
    }
}
