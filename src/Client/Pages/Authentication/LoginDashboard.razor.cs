using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using MudBlazor;
using MudExtensions;
using MudExtensions.Enums;
using RAFFLE.BlazorWebAssembly.Client.Components.Common;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.Auth;
using RAFFLE.BlazorWebAssembly.Client.Shared;
using RAFFLE.WebApi.Shared.Multitenancy;

namespace RAFFLE.BlazorWebAssembly.Client.Pages.Authentication;

public partial class LoginDashboard
{
    [CascadingParameter(Name = "AppDataService")]
    protected AppDataService AppDataService { get; set; } = default!;

    private readonly BridgeRequest _bridgeRequest = new();

    private readonly GetUserInfoRequest _raffleRequest = new();

    private GetUserInfoResponse _raffleResponse = new();

    private readonly TwilioCodeRequest _sendGridRequest = new();

    private VerificationResource _verification = new();

    [CascadingParameter]
    public Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    public IAuthenticationService AuthService { get; set; } = default!;
    [Inject]
    protected IArengusClient ArengusClient { get; set; } = default!;

    [Inject]
    protected ISevenFourSevenClient SevenFourSevenClient { get; set; } = default!;

    [Inject]
    protected ISendgridClient SendgridClient { get; set; } = default!;

    private CustomValidation? _customValidation;
    private EditContext ECBridgeForm { get; set; } = default!;

    private EditContext ECRaffleForm { get; set; } = default!;

    private EditContext ECSendgridForm { get; set; } = default!;

    private bool _bridgeStepIsActive = true;
    private bool _disableBridgeFields = false;

    private bool _raffleStepIsActive = true;
    private bool _disableRaffleFields = false;

    private bool _sendGridStepIsActive = true;
    private bool _disableSendgridFields = false;

    public bool BusySubmitting { get; set; }

    private readonly TokenRequest _tokenRequest = new();

    private string TenantId { get; set; } = string.Empty;
    private bool _passwordVisibility;
    private InputType _passwordInput = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

    [Parameter]
    public string Test1 { get; set; } = default!;
    [Parameter]
    public string Test2 { get; set; } = default!;
    [Parameter]
    public string Test3 { get; set; } = default!;
    [Parameter]
    public string Test4 { get; set; } = default!;
    [Parameter]
    public string Test5 { get; set; } = default!;

    private MudStepper _stepper { get; set; } = default!;

    private bool _checkValidationBeforeComplete = true;
    private bool _linear = true;
    private bool _mobileView = true;
    private bool _iconActionButtons = true;
    private Variant _variant = Variant.Outlined;
    private HeaderTextView _headerTextView = HeaderTextView.NewLine;
    private bool _disableAnimation = false;
    private bool _disablePreviousButton = true;
    private bool _disableNextButton = true;
    private bool _disableSkipButton = true;
    private bool _disableStepResultIndicator = false;
    private bool _addResultStep = true;
    private Color _color = Color.Primary;
    private int _activeIndex = 0;
    private bool _loading = false;
    private bool _showStaticContent = false;

    protected override void OnInitialized()
    {
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomCenter;

        ECBridgeForm = new EditContext(_bridgeRequest);

        ECRaffleForm = new EditContext(_raffleResponse);

        ECSendgridForm = new EditContext(_sendGridRequest);
    }

    private async Task<bool> CheckChange(StepChangeDirection direction)
    {
        if (_checkValidationBeforeComplete == true)
        {
            // Always allow stepping backwards, even if forms are invalid
            if (direction == StepChangeDirection.Backward)
            {
                return false;
            }

            // bridge
            if (_stepper.GetActiveIndex() == 0)
            {
                _loading = true;
                StateHasChanged();
                await Task.Delay(1000);
                _loading = false;
                StateHasChanged();

                return _bridgeStepIsActive;
            }
            // raffle
            else if (_stepper.GetActiveIndex() == 1)
            {
                _loading = true;
                StateHasChanged();
                await Task.Delay(1000);
                _loading = false;
                StateHasChanged();

                return _raffleStepIsActive;
            }
            // sendgrid
            else if (_stepper.GetActiveIndex() == 2)
            {
                _loading = true;
                StateHasChanged();
                await Task.Delay(1000);
                _loading = false;
                StateHasChanged();

                return _sendGridStepIsActive;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    private void BridgeStatusChanged(StepStatus status)
    {
        if (status == StepStatus.Completed)
        {
            _bridgeStepIsActive = false;
            _disableBridgeFields = true;
        }

        Snackbar.Add($"Bridge step {status}.", Severity.Info);
    }

    private void RaffleStatusChanged(StepStatus status)
    {
        if (status == StepStatus.Completed)
        {
            _raffleStepIsActive = false;
            _disableRaffleFields = true;
        }

        Snackbar.Add($"Raffle step {status}.", Severity.Info);
    }

    private void SendgridStatusChanged(StepStatus status)
    {
        if (status == StepStatus.Completed)
        {
            _sendGridStepIsActive = false;
            _disableSendgridFields = true;

            Snackbar.Add($"Verification step {status}.", Severity.Info);
        }

        if (status == StepStatus.Continued)
        {
            // code is not correct
            // resubmit
            _sendGridStepIsActive = true;
            _disableSendgridFields = false;
        }

        StateHasChanged();
    }

    private async Task SubmitBridgeAsync()
    {
        TenantId = MultitenancyConstants.Root.Id;

        if (await ApiHelper.ExecuteCallGuardedCustomSuppressAsync(
            () => ArengusClient.BridgeUserAsync(TenantId, _bridgeRequest), Snackbar, _customValidation) is BridgeHierarchyResponse bridgeHierarchyResponse)
        {
            Snackbar.Add($"User checked: {_bridgeRequest.UserName}, successful.", Severity.Info, config => { config.ShowCloseIcon = true; });

            // this not active anymore
            // passed validation
            // so proceed to the next step
            _bridgeStepIsActive = false;
            _disableBridgeFields = true;

            _raffleRequest.UserId = _bridgeRequest.UserId;
            _raffleRequest.AuthCode = "747Temporary747";
            _raffleRequest.Email = _bridgeRequest.Email;
            _raffleRequest.UserId747 = _bridgeRequest.UserId;
            _raffleRequest.UserName747 = _bridgeRequest.UserName;

            if (await ApiHelper.ExecuteCallGuardedCustomSuppressAsync(() => SevenFourSevenClient.RaffleUserInfoAsync(TenantId, _raffleRequest), Snackbar, _customValidation) is GetUserInfoResponse getUserInfoResponse)
            {
                if (getUserInfoResponse is { })
                {
                    // just populate the
                    // get user info fields
                    _raffleResponse = getUserInfoResponse;

                    if (_bridgeRequest.IsAgent)
                    {
                        if (_raffleResponse.AgentInfo is null)
                            _raffleResponse.AgentInfo = new AgentInfo();
                    }

                    if (!_bridgeRequest.IsAgent)
                    {
                        if (_raffleResponse.PlayerInfo is null)
                            _raffleResponse.PlayerInfo = new PlayerInfo();
                    }
                }
            }

            await _stepper.SetActiveStepByIndex(1);
        }
        else
        {
            Snackbar.Add($"{_bridgeRequest.UserName} is not a valid 747 Community user, please joined 747 thru valid channels.", Severity.Error, config =>
            {
                config.ShowCloseIcon = true;
                config.VisibleStateDuration = 20000;
            });

            _bridgeStepIsActive = true;
        }
    }

    private async Task GenerateSendgridVerificationAsync()
    {
        TenantId = MultitenancyConstants.Root.Id;

        if (_raffleResponse.Email is not null)
        {
            if (await ApiHelper.ExecuteCallGuardedCustomSuppressAsync(
            () => SendgridClient.SendCodeAsync(TenantId, _raffleResponse.Email), Snackbar, _customValidation) is VerificationResource verification)
            {
                Snackbar.Add($"Generated sid: {verification.Sid}, successful.", Severity.Info, config => { config.ShowCloseIcon = true; });

                _raffleStepIsActive = false;
                _disableRaffleFields = true;

                if (verification is not null)
                {
                    _verification = verification;
                }

                await _stepper.SetActiveStepByIndex(2);

            }
            else
            {
                Snackbar.Add($"Code verification error", Severity.Error, config =>
                {
                    config.ShowCloseIcon = true;
                    config.VisibleStateDuration = 20000;
                });

                _sendGridStepIsActive = true;
            }
        }
    }

    private async Task SendgridVerificationAsync()
    {
        TenantId = MultitenancyConstants.Root.Id;

        if (_raffleResponse.Email is not null)
        {
            if (_verification is { })
            {
                _sendGridRequest.Email = _raffleRequest.Email!;

                if (await ApiHelper.ExecuteCallGuardedCustomSuppressAsync(
                () => SendgridClient.VerifyCodeAsync(TenantId, _sendGridRequest), Snackbar, _customValidation) is VerificationCheckResource verificationCheckResource)
                {
                    Snackbar.Add($"Generated sid: {verificationCheckResource.Status}, successful.", Severity.Info, config => { config.ShowCloseIcon = true; });

                    // this not active anymore
                    // passed validation
                    // so proceed to the next step
                    _sendGridStepIsActive = false;
                    _disableSendgridFields = true;

                    await _stepper.SetActiveIndex(3);

                    await GenerateLoginDashboard(verificationCheckResource.Status);
                }
                else
                {
                    Snackbar.Add($"Code verification error", Severity.Error, config =>
                    {
                        config.ShowCloseIcon = true;
                        config.VisibleStateDuration = 20000;
                    });

                    _sendGridStepIsActive = true;
                }
            }
        }
    }

    private async Task GenerateLoginDashboard(string? VerificationStatus)
    {
        TenantId = MultitenancyConstants.Root.Id;

        // provide token
        // one time password technique
        if (VerificationStatus is not null && VerificationStatus == "approved")
        {
            if (AuthService.ProviderType == AuthProvider.AzureAd)
            {
                AuthService.NavigateToExternalLogin(Navigation.Uri);
                return;
            }

            AuthenticationState? authState = await AuthState;
            if (authState.User.Identity?.IsAuthenticated is true)
            {
                Navigation.NavigateTo("/");
            }

            if (await ApiHelper.ExecuteCallGuardedAsync(
                () => SevenFourSevenClient.SelfRegisterAsync(TenantId, new RegisterUserRequest
                {
                    IsAgent = _bridgeRequest.IsAgent,

                    AuthCode = "747Temporary747",
                    Email = _raffleResponse.Email!,
                    Info747 = new Info747
                    {
                        UniqueCode = $"747--{_sendGridRequest.Code}--747",
                        UserId747 = _raffleRequest.UserId747,
                        Username747 = _raffleRequest.UserName747
                    },
                    Name = _raffleResponse.Name!,
                    Phone = _raffleResponse.Phone!,
                    SocialProfiles = new SocialProfiles
                    {
                        FacebookUrl = string.Empty,
                        InstagramUrl = string.Empty,
                        TwitterUrl = string.Empty
                    },
                    Surname = _raffleResponse.Surname!
                }),
                Snackbar,
                _customValidation) is GenericResponse genericResponse)
            {
                _tokenRequest.Email = _raffleRequest.Email;
                _tokenRequest.Password = genericResponse.Message;

                if (await ApiHelper.ExecuteCallGuardedAsync(
                    () => AuthService.LoginAsync(TenantId, _tokenRequest),
                    Snackbar,
                    _customValidation))
                {
                    Snackbar.Add($"Logged in as {_tokenRequest.Email}", Severity.Info);

                    // get our URI
                    var uri = Navigation.ToAbsoluteUri(Navigation.Uri);

                    bool foundQueryParameter = QueryHelpers.ParseQuery(uri.Query).TryGetValue("redirect_url", out var valueFromQueryString);

                    if (foundQueryParameter)
                    {
                        string redirect_url = valueFromQueryString.FirstOrDefault() ?? string.Empty;

                        Navigation.NavigateTo(redirect_url);
                    }
                }
            }
        }
        else
        {
            Snackbar.Add($"Verification unsuccessful", Severity.Error, config =>
            {
                config.ShowCloseIcon = true;
                config.VisibleStateDuration = 20000;
            });

            _sendGridStepIsActive = true;

            SendgridStatusChanged(StepStatus.Continued);
        }
    }

}