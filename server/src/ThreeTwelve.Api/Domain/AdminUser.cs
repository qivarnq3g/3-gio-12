using Microsoft.AspNetCore.Identity;

namespace ThreeTwelve.Api.Domain;

public sealed class AdminUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}
