using System.Security.Claims;
using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Components.Dialogs;
using EHULOG.BlazorWebAssembly.Client.Components.EntityTable;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Identity.Account;

public partial class Package
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;
    [Inject]
    protected IAppUsersClient AppUsersClient { get; set; } = default!;
    [Inject]
    protected IPackagesClient PackagesClient { get; set; } = default!;
    [Inject]
    private IRolesClient RolesClient { get; set; } = default!;

    [Parameter]
    public AppUserDto AppUserDto { get; set; }

    private List<PackageDto> _packages = new ();
    private List<ExtendedPackageDto> runningPackages = new();

    private List<RoleDto> _roles = new();
    private List<ExtendedRoleDto> runningRoles = new();

    private string? _userId;

    private CustomValidation? _customValidation;

    private string selectedRole = string.Empty;

    public class SelectedVisible
    {
        public bool IsSelected { get; set; } = false;

        public bool IsVisible { get; set; } = false;
    }
    public class ExtendedPackageDto : SelectedVisible
    {
        public PackageDto PackageDto { get; set; } = default!;
    }

    public class ExtendedRoleDto : SelectedVisible
    {
        public RoleDto RoleDto { get; set; } = default!;
    }

    protected override async Task OnInitializedAsync()
    {
        var searchPackages = new SearchPackagesRequest();

        searchPackages.PackageId = null;

        var allPackages = await PackagesClient.SearchAsync(searchPackages);
        if (allPackages is not null)
        {
            _packages = allPackages.Data.ToList();

            foreach (var package in _packages)
            {
                runningPackages.Add(new ExtendedPackageDto()
                {
                    PackageDto = package,
                    IsSelected = false,
                    IsVisible = false
                });
            }
        }

        var roles = await RolesClient.GetListAsync();
        if (roles is not null)
        {
            _roles = roles.Where(r => !(new string[] { "Basic", "Admin" }).Contains(r.Name)).ToList();

            foreach (var role in _roles)
            {
                runningRoles.Add(new ExtendedRoleDto()
                {
                    RoleDto = role,
                    IsSelected = false,
                    IsVisible = true
                });
            }
        }

        if ((await AuthState).User is { } user)
        {
            _userId = user.GetUserId();

            if (_userId is not null)
            {

            }

        }
    }


    private void UpdateRole(ExtendedRoleDto extendedRoleDto)
    {
        selectedRole = extendedRoleDto.RoleDto.Name;

        foreach (var role in runningRoles)
        {
            role.IsSelected = false;
        }

        extendedRoleDto.IsSelected = true;

        if (selectedRole == "Lender")
        {
            foreach (var package in runningPackages)
            {
                package.IsSelected = false;

                if (package.PackageDto.IsLender)
                {
                    package.IsVisible = true;
                }
                else
                {
                    package.IsVisible = false;
                }
            }
        }

        AppUserDto.RoleId = extendedRoleDto.RoleDto.Id;
        System.Console.Write("RoleId: ");
        System.Console.WriteLine(extendedRoleDto.RoleDto.Id);
        StateHasChanged();
    }

    private void UpdatePackage(ExtendedPackageDto extendedPackageDto)
    {
        foreach (var package in runningPackages)
        {
            package.IsSelected = false;
        }

        extendedPackageDto.IsSelected = true;

        AppUserDto.PackageId = extendedPackageDto.PackageDto.Id;
        System.Console.Write("PackageId: ");
        System.Console.WriteLine(extendedPackageDto.PackageDto.Id);
        StateHasChanged();
    }

    private void ClearAppUserDto()
    {
        foreach (var role in runningRoles)
        {
            role.IsSelected = false;
        }

        foreach (var package in runningPackages)
        {
            package.IsSelected = false;
        }


        AppUserDto.RoleId = null;
        selectedRole = string.Empty;
        AppUserDto.PackageId = default!;
        StateHasChanged();
    }

    private void SubmitAppUserDto()
    {
        AppUserDto.RoleId = null;
        selectedRole = string.Empty;
        AppUserDto.PackageId = default!;
        StateHasChanged();
    }
}