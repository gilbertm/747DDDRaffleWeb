﻿using EHULOG.WebApi.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;

public class MustHavePermissionAttribute : AuthorizeAttribute
{
    public MustHavePermissionAttribute(string action, string resource) =>
        Policy = EHULOGPermission.NameFor(action, resource);
}