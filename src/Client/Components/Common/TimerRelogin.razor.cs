using RAFFLE.BlazorWebAssembly.Client.Infrastructure.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;

namespace RAFFLE.BlazorWebAssembly.Client.Components.Common;

public partial class TimerRelogin
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;
}
