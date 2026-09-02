using System.Security.Cryptography;
using GramShopPOS.Application.DTOs.Auth;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly IJwtTokenService _jwt;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;
    private readonly IAppEnvironment _environment;

    public AuthService(
        IAppDbContext db,
        IPasswordService passwords,
        IJwtTokenService jwt,
        ICurrentUser currentUser,
        IAuditService audit,
        IAppEnvironment environment)
    {
        _db = db;
        _passwords = passwords;
        _jwt = jwt;
        _currentUser = currentUser;
        _audit = audit;
        _environment = environment;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.StoreUsers).ThenInclude(su => su.Store)
            .FirstOrDefaultAsync(u => u.UserName == request.UserName && !u.IsDeleted, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAppException("Invalid username or password.");
        }

        if (user.LockoutEndUtc is not null && user.LockoutEndUtc > DateTime.Now)
        {
            throw new ForbiddenAppException("Account is locked. Try again later.");
        }

        if (!_passwords.Verify(request.Password, user.PasswordHash))
        {
            user.AccessFailedCount += 1;
            if (user.AccessFailedCount >= PasswordRules.LockoutThreshold)
            {
                user.LockoutEndUtc = DateTime.Now.AddMinutes(PasswordRules.LockoutMinutes);
                user.AccessFailedCount = 0;
            }

            await _db.SaveChangesAsync(cancellationToken);
            await _audit.LogAsync(AuditActions.LoginFailed, nameof(ApplicationUser), user.Id.ToString(), null, null, null, cancellationToken);
            throw new UnauthorizedAppException("Invalid username or password.");
        }

        var role = user.UserRoles.Select(x => x.Role.Name).FirstOrDefault() ?? Roles.SalesPerson;
        var stores = user.StoreUsers
            .Where(x => x.Store.IsActive && !x.Store.IsDeleted)
            .Select(x => new AssignedStoreDto
            {
                StoreId = x.StoreId,
                StoreCode = x.Store.StoreCode,
                StoreName = x.Store.StoreName,
                IsPrimary = x.IsPrimary
            })
            .ToList();

        if (role == Roles.SalesPerson && stores.Count == 0)
        {
            throw new ForbiddenAppException("Sales person is not assigned to any store.");
        }

        user.AccessFailedCount = 0;
        user.LockoutEndUtc = null;
        user.LastLoginDate = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);

        var token = _jwt.CreateToken(user.Id, user.UserName, role, stores.Select(s => s.StoreId).ToList());
        await _audit.LogAsync(AuditActions.Login, nameof(ApplicationUser), user.Id.ToString(), null, new { user.UserName }, stores.FirstOrDefault()?.StoreId, cancellationToken);

        return new LoginResponse
        {
            AccessToken = token.Token,
            Expiration = token.Expiration,
            UserId = user.Id,
            UserName = user.UserName,
            Role = role,
            AssignedStores = stores,
            MustChangePassword = user.MustChangePassword
        };
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        if (!string.IsNullOrWhiteSpace(_currentUser.JwtId))
        {
            _db.RevokedTokens.Add(new RevokedToken
            {
                Jti = _currentUser.JwtId,
                UserId = _currentUser.UserId,
                CreatedDate = DateTime.Now,
                ExpiresAtUtc = DateTime.Now.AddHours(12)
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        await _audit.LogAsync(AuditActions.Logout, nameof(ApplicationUser), _currentUser.UserId.ToString(), null, null, null, cancellationToken);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.UserId && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("User not found.");

        if (!_passwords.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new ValidationAppException("Current password is incorrect.");
        }

        _passwords.ValidateStrength(request.NewPassword);
        user.PasswordHash = _passwords.Hash(request.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.PasswordChanged, nameof(ApplicationUser), user.Id.ToString(), null, null, null, cancellationToken);
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName && !u.IsDeleted, cancellationToken);
        var response = new ForgotPasswordResponse
        {
            Message = "If the account exists, a password reset token has been generated."
        };

        if (user is null)
        {
            return response;
        }

        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = _passwords.Hash(raw),
            CreatedDate = DateTime.Now,
            ExpiresAtUtc = DateTime.Now.AddHours(1)
        });
        await _db.SaveChangesAsync(cancellationToken);

        if (_environment.IsDevelopment)
        {
            response.DevelopmentResetToken = raw;
        }

        return response;
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName && !u.IsDeleted, cancellationToken)
            ?? throw new ValidationAppException("Invalid reset request.");

        var tokens = await _db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAtUtc > DateTime.Now)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync(cancellationToken);

        var match = tokens.FirstOrDefault(t => _passwords.Verify(request.Token, t.TokenHash));
        if (match is null)
        {
            throw new ValidationAppException("Invalid or expired reset token.");
        }

        _passwords.ValidateStrength(request.NewPassword);
        match.IsUsed = true;
        user.PasswordHash = _passwords.Hash(request.NewPassword);
        user.MustChangePassword = false;
        user.LockoutEndUtc = null;
        user.AccessFailedCount = 0;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.PasswordReset, nameof(ApplicationUser), user.Id.ToString(), null, null, null, cancellationToken);
    }

    public async Task<CurrentUserDto> GetMeAsync(CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.StoreUsers).ThenInclude(su => su.Store)
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
            ?? throw new NotFoundAppException("User not found.");

        return new CurrentUserDto
        {
            UserId = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            Role = user.UserRoles.Select(x => x.Role.Name).FirstOrDefault() ?? _currentUser.Role,
            MustChangePassword = user.MustChangePassword,
            AssignedStores = user.StoreUsers.Select(x => new AssignedStoreDto
            {
                StoreId = x.StoreId,
                StoreCode = x.Store.StoreCode,
                StoreName = x.Store.StoreName,
                IsPrimary = x.IsPrimary
            }).ToList()
        };
    }
}
