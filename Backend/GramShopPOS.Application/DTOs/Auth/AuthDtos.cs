namespace GramShopPOS.Application.DTOs.Auth;

public class LoginRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public IReadOnlyList<AssignedStoreDto> AssignedStores { get; set; } = [];
    public bool MustChangePassword { get; set; }
}

public class AssignedStoreDto
{
    public int StoreId { get; set; }
    public string StoreCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string UserName { get; set; } = string.Empty;
}

public class ForgotPasswordResponse
{
    public string Message { get; set; } = string.Empty;
    public string? DevelopmentResetToken { get; set; }
}

public class ResetPasswordRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class CurrentUserDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public IReadOnlyList<AssignedStoreDto> AssignedStores { get; set; } = [];
    public bool MustChangePassword { get; set; }
}
