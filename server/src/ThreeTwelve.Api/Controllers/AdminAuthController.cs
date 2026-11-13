using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ThreeTwelve.Api.Auth;
using ThreeTwelve.Api.Contracts;
using ThreeTwelve.Api.Domain;
using ThreeTwelve.Api.RateLimiting;

namespace ThreeTwelve.Api.Controllers;

[ApiController]
[Route("api/admin/auth")]
[Authorize]
public sealed class AdminAuthController(
    UserManager<AdminUser> users,
    SignInManager<AdminUser> signIn,
    TimeProvider clock) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AdminLogin)]
    public async Task<ActionResult<AdminAccountResponse>> Login(AdminLoginRequest request)
    {
        ModelState.CheckEmail(request.Email);
        if (request.Password is null || request.Password.Length > AdminAuthExtensions.MaxPasswordLength)
            ModelState.AddModelError("password", $"Bắt buộc, tối đa {AdminAuthExtensions.MaxPasswordLength} ký tự.");
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await users.FindByEmailAsync(request.Email!);
        if (user is null || !user.IsActive)
            return LoginFailed();

        var result = await signIn.PasswordSignInAsync(user, request.Password!, isPersistent: false, lockoutOnFailure: true);
        if (!result.Succeeded)
            return LoginFailed();

        user.LastLoginAt = clock.GetUtcNow();
        await users.UpdateAsync(user);
        return AdminAccountResponse.From(user);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<ActionResult<AdminAccountResponse>> Me()
    {
        var user = await users.GetUserAsync(User);
        if (user is null || !user.IsActive)
            return Unauthorized();
        return AdminAccountResponse.From(user);
    }

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (request.CurrentPassword is null || request.CurrentPassword.Length > AdminAuthExtensions.MaxPasswordLength)
            ModelState.AddModelError("currentPassword", "Bắt buộc.");
        ModelState.CheckNewPassword(request.NewPassword, "newPassword");
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await users.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var result = await users.ChangePasswordAsync(user, request.CurrentPassword!, request.NewPassword!);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("currentPassword", "Mật khẩu hiện tại không đúng.");
            return ValidationProblem(ModelState);
        }

        await signIn.RefreshSignInAsync(user);
        return NoContent();
    }

    private ObjectResult LoginFailed() =>
        Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sai email hoặc mật khẩu",
            detail: "Kiểm lại email và mật khẩu. Sai 5 lần liên tiếp thì phải chờ 15 phút.");
}
