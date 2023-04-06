using RAFFLE.WebApi.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace RAFFLE.BlazorWebAssembly.Client.Infrastructure.Auth;

public class MustHavePermissionAttribute : AuthorizeAttribute
{
    public MustHavePermissionAttribute(string action, string resource) =>
        Policy = RAFFLEPermission.NameFor(action, resource);
}