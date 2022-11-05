using EHULOG.BlazorWebAssembly.Client.Components.EntityTable;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Shared;
using EHULOG.WebApi.Shared.Authorization;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Administration.User;

public partial class FilesDocuments
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthorizationService AuthService { get; set; } = default!;
    [Inject]
    protected AppDataService AppDataService { get; set; } = default!;
    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    [Inject]
    protected IUsersClient UsersClient { get; set; } = default!;

    [Inject]
    protected IAppUsersClient AppUsersClient { get; set; } = default!;

    protected List<UserDetailsExtendedDto> ApplicationUsers { get; set; } = default!;

    protected EntityClientTableContext<UserDetailsExtendedDto, Guid, CreateUserRequest> Context { get; set; } = default!;

    private bool _canExportUsers;
    private bool _canViewRoles;

    protected override async Task OnInitializedAsync()
    {
        var user = (await AuthState).User;
        _canExportUsers = await AuthService.HasPermissionAsync(user, EHULOGAction.Export, EHULOGResource.Users);
        _canViewRoles = await AuthService.HasPermissionAsync(user, EHULOGAction.View, EHULOGResource.UserRoles);

        LoadContext();
    }

    public void LoadContext()
    {
        Context = new(
           entityName: L["User documents for verification"],
           entityNamePlural: L["Users and documents for verification"],
           entityResource: EHULOGResource.Users,
           searchAction: EHULOGAction.View,
           updateAction: string.Empty,
           deleteAction: string.Empty,
           fields: new()
           {
                new (user => user.Id, L["Application users"], string.Empty, null, TemplateUserInfo),
                new (user => user.Role, L["Role"], L["Role"], Type: typeof(string)),
                new (user => user.IsActive, L["Active"], string.Empty, Type: typeof(bool)),
                new EntityField<UserDetailsExtendedDto>(user => user.Verified, L["Verified"], L["Verified"], Type: typeof(bool)),
                new (user => user.Id, L["Files and documents"], string.Empty, null, FilesAndDocuments)
           },
           idFunc: user => user.Id,
           loadDataFunc: async () =>
           {
               return ApplicationUsers = await LoadData();
           },
           searchFunc: (searchString, user) =>
           string.IsNullOrWhiteSpace(searchString) || user.FirstName?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true || user.LastName?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true
           || user.Email?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true
           || user.PhoneNumber?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true
           || user.UserName?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true,
           createAction: string.Empty,
           exportAction: string.Empty
       );
    }

    private async Task<List<UserDetailsExtendedDto>> LoadData()
    {
        var users = await UsersClient.GetListAsync();

        if (users == default)
        {
            return default!;
        }

        List<UserDetailsExtendedDto> userDetailsWithImages = new();

        foreach (var user in users.Where(u => u.IsActive))
        {
            bool isQualifiedUser = false;
            bool isApplicationVerifiedUser = false;
            bool isForDocumentVerificationStatus = false;
            string applicationRole = string.Empty;

            if (await ApiHelper.ExecuteCallGuardedCustomSuppressAsync(
            () => UsersClient.GetRolesAsync(user.Id.ToString()), Snackbar)
        is ICollection<UserRoleDto> response)
            {
                var roles = response.ToList();

                foreach (var role in roles)
                {
                    // if the user is the user of the system
                    // and already enabled
                    if (new string[] { "Lender", "Lessee" }.Contains(role.RoleName) && role.Enabled)
                    {
                        isQualifiedUser = true;
                        applicationRole = string.IsNullOrEmpty(role.RoleName) ? string.Empty : role.RoleName;
                        continue;
                    }
                }
            }

            if (await ApiHelper.ExecuteCallGuardedCustomSuppressAsync(
            () => AppUsersClient.GetAsync(user.Id.ToString()), Snackbar)
        is AppUserDto appUserDto)
            {
                if (appUserDto != default)
                {
                    if (appUserDto.IsVerified)
                    {
                        isApplicationVerifiedUser = true;
                    }

                    if (appUserDto.DocumentsStatus == VerificationStatus.Submitted)
                    {
                        isForDocumentVerificationStatus = true;
                    }
                }
            }

            // qualifed user and submitted the document for verification
            if (isQualifiedUser && !isApplicationVerifiedUser && isForDocumentVerificationStatus)
            {
                var qualifiedUser = user.Adapt<UserDetailsExtendedDto>();

                if (qualifiedUser != null)
                {
                    qualifiedUser.Role = applicationRole;
                    qualifiedUser.Verified = isApplicationVerifiedUser;
                    qualifiedUser.DocumentVerificationStatus = isForDocumentVerificationStatus;

                    if (await ApiHelper.ExecuteCallGuardedCustomSuppressAsync(() => InputOutputResourceClient.GetAsync(user.Id), Snackbar) is ICollection<InputOutputResourceDto> inputOutputFilesAndDocuments)
                    {
                        if (inputOutputFilesAndDocuments != default)
                        {
                            if (inputOutputFilesAndDocuments.Count > 0)
                            {
                                qualifiedUser.FilesOrDocumentsForVerification = inputOutputFilesAndDocuments.ToList();
                            }
                        }
                    }

                    userDetailsWithImages.Add(qualifiedUser);
                }
            }
        }

        return userDetailsWithImages;
    }
}

public class UserDetailsExtendedDto : UserDetailsDto
{
    public bool Verified { get; set; } = false;

    public bool DocumentVerificationStatus { get; set; } = false;

    public string Role { get; set; } = string.Empty;

    public List<InputOutputResourceDto> FilesOrDocumentsForVerification { get; set; } = default!;
}