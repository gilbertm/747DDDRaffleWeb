using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Components.EntityContainer;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Shared;
using EHULOG.WebApi.Shared.Multitenancy;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Net.Http.Headers;
using System.Text.Json;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Authentication;

public partial class SelfRegister
{
    [Parameter]
    public bool IsFieldsOnly { get; set; } = false;

    private readonly CreateUserRequest _createUserRequest = new();
    private CustomValidation? _customValidation;
    private bool BusySubmitting { get; set; }

    [Inject]
    private IUsersClient UsersClient { get; set; } = default!;

    [Inject]
    private HttpClient HttpClient { get; set; } = default!;

    private string Tenant { get; set; } = MultitenancyConstants.Root.Id;

    private bool _passwordVisibility;
    private InputType _passwordInput = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

    private string? successMessage;

    private async Task SubmitAsync()
    {
        // NOTE: custom bypass on
        // Client.Infrastructure\Auth\Jwt\JwtAuthenticationHeaderHandler.cs
        // this enables not redirecting to somewhere else
        // to complete the textual ui journey of what's happening after registration
        BusySubmitting = true;

        successMessage = await ApiHelper.ExecuteCallGuardedAsync(
            () => UsersClient.SelfRegisterAsync(Tenant, _createUserRequest),
            Snackbar,
            _customValidation);

        if (successMessage != null)
        {
            Snackbar.Add(successMessage, Severity.Info);
            // see note above why disabled
            // Navigation.NavigateTo("/login");
        }

        BusySubmitting = false;
    }

    private async Task SubmitCustomAsync()
    {
        HttpRequestMessage request;
        HttpResponseMessage response;

        request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Config[ConfigNames.ApiBaseUrl]}api/tokens/"),
            Content = new StringContent(JsonSerializer.Serialize(new TokenRequest()
            {
                Email = MultitenancyConstants.Root.EmailAddress,
                Password = MultitenancyConstants.DefaultPassword
            })),
        };

        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Content.Headers.Add("tenant", "ehulog");

        response = await HttpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse is not null)
            {
                request = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"{Config[ConfigNames.ApiBaseUrl]}api/users"),
                    Content = new StringContent(JsonSerializer.Serialize(_createUserRequest)),
                };

                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.Token);

                request.Content.Headers.Add("tenant", "ehulog");

                response = await HttpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine(response);
                }
            }
        }
    }

    private void TogglePasswordVisibility()
    {
        if (_passwordVisibility)
        {
            _passwordVisibility = false;
            _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
            _passwordInput = InputType.Password;
        }
        else
        {
            _passwordVisibility = true;
            _passwordInputIcon = Icons.Material.Filled.Visibility;
            _passwordInput = InputType.Text;
        }
    }
}