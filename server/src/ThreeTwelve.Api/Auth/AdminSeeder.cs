using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ThreeTwelve.Api.Domain;

namespace ThreeTwelve.Api.Auth;

public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        var users = services.GetRequiredService<UserManager<AdminUser>>();
        if (await users.Users.AnyAsync())
            return;

        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            logger.LogWarning("Chưa có tài khoản quản trị nào và thiếu Admin__Email, Admin__Password; trang quản trị chưa đăng nhập được.");
            return;
        }

        var admin = new AdminUser
        {
            UserName = email,
            Email = email,
            DisplayName = configuration["Admin:DisplayName"] ?? "Quản trị viên",
            CreatedAt = services.GetRequiredService<TimeProvider>().GetUtcNow(),
        };
        var result = await users.CreateAsync(admin, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                "Không tạo được tài khoản quản trị đầu tiên: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        logger.LogInformation("Đã tạo tài khoản quản trị đầu tiên {Email}.", email);
    }
}
