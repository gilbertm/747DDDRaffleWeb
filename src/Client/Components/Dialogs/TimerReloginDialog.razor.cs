﻿using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;

namespace EHULOG.BlazorWebAssembly.Client.Shared.Dialogs;

public partial class TimerReloginDialog
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;

}
