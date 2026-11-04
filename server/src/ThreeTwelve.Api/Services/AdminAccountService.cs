using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ThreeTwelve.Api.Contracts;
using ThreeTwelve.Api.Data;
using ThreeTwelve.Api.Domain;

namespace ThreeTwelve.Api.Services;

public interface IAdminAccountService
{
    Task<List<AdminUser>> ListAsync(CancellationToken ct);
    Task<AdminUser> CreateAsync(CreateAdminAccountRequest request, CancellationToken ct);
    Task<AdminUser> UpdateAsync(Guid id, UpdateAdminAccountRequest request, Guid actorId, CancellationToken ct);
    Task ResetPasswordAsync(Guid id, string newPassword, CancellationToken ct);
    Task DeleteAsync(Guid id, Guid actorId, CancellationToken ct);
}

public sealed class AdminAccountService(AppDbContext db, UserManager<AdminUser> users, TimeProvider clock) : IAdminAccountService
{
    private const long AccountsLockKey = 3_12_0001;

    public Task<List<AdminUser>> ListAsync(CancellationToken ct) =>
        users.Users.AsNoTracking().OrderBy(u => u.CreatedAt).ToListAsync(ct);

    public async Task<AdminUser> CreateAsync(CreateAdminAccountRequest request, CancellationToken ct)
    {
        await using var transaction = await LockAccountsAsync(ct);
        if (await users.FindByEmailAsync(request.Email!) is not null)
            throw new ConflictException("Email này đã có tài khoản.");

        var user = new AdminUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName!.Trim(),
            CreatedAt = clock.GetUtcNow(),
        };
        Ensure(await users.CreateAsync(user, request.Password!));
        await transaction.CommitAsync(ct);
        return user;
    }

    public async Task<AdminUser> UpdateAsync(Guid id, UpdateAdminAccountRequest request, Guid actorId, CancellationToken ct)
    {
        await using var transaction = await LockAccountsAsync(ct);
        var user = await FindAsync(id);

        if (request.IsActive == false && user.IsActive)
        {
            if (id == actorId)
                throw new ConflictException("Không tự khóa tài khoản của mình.");
            await EnsureAnotherActiveAsync(id, ct);
        }

        if (request.DisplayName is not null)
            user.DisplayName = request.DisplayName.Trim();
        var deactivating = request.IsActive == false && user.IsActive;
        if (request.IsActive is { } isActive)
            user.IsActive = isActive;

        Ensure(await users.UpdateAsync(user));
        if (deactivating)
            Ensure(await users.UpdateSecurityStampAsync(user));
        await transaction.CommitAsync(ct);
        return user;
    }

    public async Task ResetPasswordAsync(Guid id, string newPassword, CancellationToken ct)
    {
        var user = await FindAsync(id);
        Ensure(await users.RemovePasswordAsync(user));
        Ensure(await users.AddPasswordAsync(user, newPassword));
        Ensure(await users.ResetAccessFailedCountAsync(user));
        Ensure(await users.SetLockoutEndDateAsync(user, null));
    }

    public async Task DeleteAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        await using var transaction = await LockAccountsAsync(ct);
        var user = await FindAsync(id);
        if (id == actorId)
            throw new ConflictException("Không tự xóa tài khoản của mình.");
        if (user.IsActive)
            await EnsureAnotherActiveAsync(id, ct);

        Ensure(await users.DeleteAsync(user));
        await transaction.CommitAsync(ct);
    }

    private async Task<IDbContextTransaction> LockAccountsAsync(CancellationToken ct)
    {
        var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlAsync($"select pg_advisory_xact_lock({AccountsLockKey})", ct);
        return transaction;
    }

    private async Task<AdminUser> FindAsync(Guid id) =>
        await users.FindByIdAsync(id.ToString()) ?? throw new NotFoundException("Không có tài khoản này.");

    private async Task EnsureAnotherActiveAsync(Guid id, CancellationToken ct)
    {
        if (!await users.Users.AnyAsync(u => u.IsActive && u.Id != id, ct))
            throw new ConflictException("Phải còn ít nhất một tài khoản đang hoạt động.");
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
