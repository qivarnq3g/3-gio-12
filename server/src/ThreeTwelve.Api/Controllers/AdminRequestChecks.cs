using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ThreeTwelve.Api.Auth;

namespace ThreeTwelve.Api.Controllers;

public static class AdminRequestChecks
{
    private static readonly EmailAddressAttribute EmailFormat = new();

    public static void CheckEmail(this ModelStateDictionary state, string? email, string key = "email")
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 256 || !EmailFormat.IsValid(email))
            state.AddModelError(key, "Bắt buộc, email hợp lệ, tối đa 256 ký tự.");
    }

    public static void CheckNewPassword(this ModelStateDictionary state, string? password, string key)
    {
        if (password is null || password.Length is < AdminAuthExtensions.MinPasswordLength or > AdminAuthExtensions.MaxPasswordLength)
            state.AddModelError(key, $"Từ {AdminAuthExtensions.MinPasswordLength} tới {AdminAuthExtensions.MaxPasswordLength} ký tự.");
    }

    public static void CheckDisplayName(this ModelStateDictionary state, string? displayName, bool required)
    {
        if (displayName is null ? required : displayName.Trim().Length is 0 or > 64)
            state.AddModelError("displayName", "Từ 1 tới 64 ký tự.");
    }
}
