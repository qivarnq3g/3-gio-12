using System.Text.Json.Serialization;
using ThreeTwelve.Api.Domain;

namespace ThreeTwelve.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AdminLoginRequest(string? Email, string? Password);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateAdminAccountRequest(string? Email, string? DisplayName, string? Password);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateAdminAccountRequest(string? DisplayName, bool? IsActive);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ResetPasswordRequest(string? NewPassword);

public sealed record AdminAccountResponse(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt)
{
    public static AdminAccountResponse From(AdminUser user) => new(
        user.Id,
        user.Email!,
        user.DisplayName,
        user.IsActive,
        user.CreatedAt.UtcDateTime,
        user.LastLoginAt?.UtcDateTime);
}
