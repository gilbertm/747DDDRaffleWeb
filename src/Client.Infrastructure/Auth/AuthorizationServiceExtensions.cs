using RAFFLE.WebApi.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace RAFFLE.BlazorWebAssembly.Client.Infrastructure.Auth;

public static class AuthorizationServiceExtensions
{
    public static async Task<bool> HasPermissionAsync(this IAuthorizationService service, ClaimsPrincipal user, string action, string resource) =>
        (await service.AuthorizeAsync(user, null, RAFFLEPermission.NameFor(action, resource))).Succeeded;
}