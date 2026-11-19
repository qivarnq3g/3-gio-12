using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ThreeTwelve.Api.Contracts;
using ThreeTwelve.Api.Domain;
using ThreeTwelve.Api.Services;

namespace ThreeTwelve.Api.Controllers;

[ApiController]
[Route("api/admin/accounts")]
[Authorize]
public sealed class AdminAccountsController(IAdminAccountService accounts, UserManager<AdminUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<List<AdminAccountResponse>> List(CancellationToken ct) =>
        (await accounts.ListAsync(ct)).Select(AdminAccountResponse.From).ToList();

    [HttpPost]
    public async Task<ActionResult<AdminAccountResponse>> Create(CreateAdminAccountRequest request, CancellationToken ct)
    {
        ModelState.CheckEmail(request.Email);
        ModelState.CheckDisplayName(request.DisplayName, required: true);
        ModelState.CheckNewPassword(request.Password, "password");
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await accounts.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, AdminAccountResponse.From(user));
    }

    [HttpPatch("{accountId}")]
    public async Task<ActionResult<AdminAccountResponse>> Update(Guid accountId, UpdateAdminAccountRequest request, CancellationToken ct)
    {
        if (request.DisplayName is null && request.IsActive is null)
            ModelState.AddModelError("", "Cần ít nhất một trong displayName, isActive.");
        ModelState.CheckDisplayName(request.DisplayName, required: false);
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        return AdminAccountResponse.From(await accounts.UpdateAsync(accountId, request, ActorId(), ct));
    }

    [HttpPost("{accountId}/password")]
    public async Task<IActionResult> ResetPassword(Guid accountId, ResetPasswordRequest request, CancellationToken ct)
    {
        if (accountId == ActorId())
            ModelState.AddModelError("accountId", "Đổi mật khẩu của chính mình dùng /api/admin/auth/password.");
        ModelState.CheckNewPassword(request.NewPassword, "newPassword");
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        await accounts.ResetPasswordAsync(accountId, request.NewPassword!, ct);
        return NoContent();
    }

    [HttpDelete("{accountId}")]
    public async Task<IActionResult> Delete(Guid accountId, CancellationToken ct)
    {
        await accounts.DeleteAsync(accountId, ActorId(), ct);
        return NoContent();
    }

    private Guid ActorId() => Guid.Parse(users.GetUserId(User)!);
}
