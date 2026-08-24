using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace GramShopPOS.Application.Services;

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<ApplicationUser> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(new ApplicationUser(), password);

    public bool Verify(string password, string hash)
    {
        var result = _hasher.VerifyHashedPassword(new ApplicationUser(), hash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    public void ValidateStrength(string password)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(password) || password.Length < PasswordRules.MinLength)
        {
            errors.Add($"Password must be at least {PasswordRules.MinLength} characters.");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Password must contain an uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add("Password must contain a lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Password must contain a number.");
        }

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            errors.Add("Password must contain a special character.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException("Password does not meet complexity requirements.", errors);
        }
    }
}
