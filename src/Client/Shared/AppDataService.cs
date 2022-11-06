using Microsoft.JSInterop;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using Microsoft.AspNetCore.Components.Authorization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Geo.MapBox.Abstractions;
using MudBlazor;
using Microsoft.AspNetCore.Components;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Pages.Identity.Users;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Nager.Country;
using EHULOG.BlazorWebAssembly.Client.Components.Common;

namespace EHULOG.BlazorWebAssembly.Client.Shared;

public class AppDataService : IAppDataService
{
    private IGeolocationService GeolocationService { get; set; } = default!;

    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private IHttpClientFactory HttpClientFactory { get; set; } = default!;

    private IConfiguration Configuration { get; set; } = default!;

    private IAppUsersClient AppUsersClient { get; set; } = default!;

    private IPackagesClient PackagesClient { get; set; } = default!;

    private IRolesClient RolesClient { get; set; } = default!;
    private ILoansClient LoansClient { get; set; } = default!;

    private IUsersClient UsersClient { get; set; } = default!;

    private IMapBoxGeocoding MapBoxGeocoding { get; set; } = default!;

    private ISnackbar Snackbar { get; set; } = default!;

    public AppDataService(IGeolocationService geolocationService, AuthenticationStateProvider authenticationStateProvider, ISnackbar snackbar, IMapBoxGeocoding mapBoxGeocoding, IConfiguration configuration, IAppUsersClient appUsersClient, IPackagesClient packagesClient, IUsersClient usersClient, IRolesClient rolesClient, IHttpClientFactory httpClientFactory, ILoansClient loansClient)
    {
        GeolocationService = geolocationService;

        HttpClientFactory = httpClientFactory;

        Snackbar = snackbar;

        Configuration = configuration;

        MapBoxGeocoding = mapBoxGeocoding;

        AppUsersClient = appUsersClient;

        UsersClient = usersClient;

        PackagesClient = packagesClient;

        RolesClient = rolesClient;

        AuthenticationStateProvider = authenticationStateProvider;

        LoansClient = loansClient;

    }

    public Task Initialization { get; private set; } = default!;

    private readonly PositionOptions _options = new()
    {
        EnableHighAccuracy = true,
        MaximumAge = null,
        Timeout = 15_000
    };

    private AppUserDto? _appUserDto;

    public AppUserDto AppUser
    {
        get
        {
            return _appUserDto ?? default!;
        }

        private set
        {
            if (value != default!)
            {
                if (!AppUserDto.Equals(_appUserDto, value))
                {
                    _appUserDto = value;
                }
            }
        }
    }

    private bool IsNewUser { get; set; } = false;

    private GeolocationPosition? _position;

    private GeolocationPositionError? _positionError;

    public async Task InitializationAsync()
    {
        if ((await AuthenticationStateProvider.GetAuthenticationStateAsync()).User is { } userClaimsPrincipal)
        {
            GeolocationService.GetCurrentPosition(
                   component: this,
                   onSuccessCallbackMethodName: nameof(OnPositionRecieved),
                   onErrorCallbackMethodName: nameof(OnPositionError),
                   options: _options);

            string userId = userClaimsPrincipal.GetUserId() ?? string.Empty;

            if (!string.IsNullOrEmpty(userId))
            {
                // if not found / not found exception
                // create a silent custom helper
                var appUserClient = await ApiHelper.ExecuteCallGuardedCustomSuppressAsync(async () => await AppUsersClient.GetAsync(userId), Snackbar, default);

                // the get async is throwing errors
                // the user doesn't not exist
                // create init
                if (appUserClient == default)
                {
                    var createAppUserRequest = new CreateAppUserRequest
                    {
                        ApplicationUserId = userId
                    };

                    if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.CreateAsync(createAppUserRequest), Snackbar, default!, "Personal application profile created") is Guid guid)
                    {
                        IsNewUser = true;
                    }
                }

                // the app user should be present here
                AppUser = await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.GetAsync(userId), Snackbar, default!) ?? default!;

                if (AppUser != default)
                {
                    if (IsNewUser)
                    {
                        if (_position != default)
                        {
                            AppUser.Longitude = _position.Coords.Longitude.ToString();
                            AppUser.Latitude = _position.Coords.Latitude.ToString();

                            var responseReverseGeocoding = await MapBoxGeocoding.ReverseGeocodingAsync(new()
                            {
                                Coordinate = new Geo.MapBox.Models.Coordinate()
                                {
                                    Latitude = Convert.ToDouble(AppUser.Latitude),
                                    Longitude = Convert.ToDouble(AppUser.Longitude)
                                },
                                EndpointType = Geo.MapBox.Enums.EndpointType.Places
                            });

                            if (responseReverseGeocoding != default)
                            {
                                if (responseReverseGeocoding.Features != default)
                                {
                                    if (responseReverseGeocoding.Features.Count > 0)
                                    {
                                        foreach (var f in responseReverseGeocoding.Features)
                                        {
                                            if (f.Contexts.Count > 0)
                                            {
                                                foreach (var c in f.Contexts)
                                                {
                                                    // System.Diagnostics.Debug.Write(c.Id.Contains("Home"));
                                                    // System.Diagnostics.Debug.Write(c.ContextText);

                                                    if (!string.IsNullOrEmpty(c.Id))
                                                    {
                                                        switch (c.Id)
                                                        {
                                                            case string s when s.Contains("country"):
                                                                AppUser.HomeCountry = c.ContextText[0].Text;
                                                                break;
                                                            case string s when s.Contains("region"):
                                                                AppUser.HomeRegion = c.ContextText[0].Text;
                                                                break;
                                                            case string s when s.Contains("postcode"):
                                                                AppUser.HomeAddress += c.ContextText[0].Text;
                                                                break;
                                                            case string s when s.Contains("district"):
                                                                AppUser.HomeCountry += c.ContextText[0].Text;
                                                                break;
                                                            case string s when s.Contains("place"):
                                                                AppUser.HomeCity = c.ContextText[0].Text;
                                                                break;
                                                            case string s when s.Contains("locality"):
                                                                AppUser.HomeAddress += c.ContextText[0].Text;
                                                                break;
                                                            case string s when s.Contains("neighborhood"):
                                                                AppUser.HomeAddress += c.ContextText[0].Text;
                                                                break;
                                                            case string s when s.Contains("address"):
                                                                AppUser.HomeAddress += c.ContextText[0].Text;
                                                                break;
                                                            case string s when s.Contains("poi"):
                                                                AppUser.HomeAddress += c.ContextText[0].Text;
                                                                break;
                                                        }
                                                    }
                                                }
                                            }

                                            if (f.Center is { })
                                            {
                                                AppUser.Latitude = f.Center.Latitude.ToString();
                                                AppUser.Longitude = f.Center.Longitude.ToString();
                                            }

                                            if (f.Properties.Address is { })
                                            {
                                                AppUser.HomeAddress += f.Properties.Address;
                                            }
                                        }
                                    }

                                }
                            }
                        }

                        var updateAppUserRequest = new UpdateAppUserRequest
                        {
                            ApplicationUserId = AppUser.ApplicationUserId,
                            HomeAddress = AppUser.HomeAddress,
                            HomeCity = AppUser.HomeCity,
                            HomeCountry = AppUser.HomeCountry,
                            HomeRegion = AppUser.HomeRegion,
                            Id = AppUser.Id,
                            IsVerified = AppUser.IsVerified,
                            Latitude = AppUser.Latitude,
                            Longitude = AppUser.Longitude,
                            PackageId = AppUser.PackageId
                        };

                        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.UpdateAsync(AppUser.Id, updateAppUserRequest), Snackbar, null) is Guid guid)
                        {
                            AppUser = await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.GetAsync(userId), Snackbar, null) ?? default!;
                        }
                    }

                    // get application user role, the selected if exists
                    // if not just assign the other roles
                    // this is used for checking on some parts of the system.
                    var userRoles = await UsersClient.GetRolesAsync(userId);
                    if (userRoles != null)
                    {
                        if (!string.IsNullOrEmpty(AppUser.RoleId))
                        {
                            if (_appUserDto is { })
                            {
                                var userRole = userRoles.FirstOrDefault(ur => (ur.RoleId is not null) && ur.RoleId.Equals(AppUser.RoleId) && ur.Enabled);

                                if (userRole is not null)
                                {
                                    AppUser.RoleId = userRole.RoleId;
                                    AppUser.RoleName = userRole.RoleName;
                                }
                            }
                        }
                        else
                        {
                            var lenderOrLessee = userRoles.Where(r => (new string[] { "Lender", "Lessee" }).Contains(r.RoleName) && r.Enabled).ToList();
                            if (lenderOrLessee.Count == 1)
                            {
                                // assigned properly with one application role
                                AppUser.RoleId = lenderOrLessee[0].RoleId;
                                AppUser.RoleName = lenderOrLessee[0].RoleName;
                            }

                            var basic = userRoles.Where(r => (new string[] { "Basic" }).Contains(r.RoleName) && r.Enabled).ToList();
                            if (basic.Count > 0)
                            {
                                AppUser.RoleId = basic[0].RoleId;
                                AppUser.RoleName = basic[0].RoleName;
                            }

                            var admin = userRoles.Where(r => (new string[] { "Admin" }).Contains(r.RoleName) && r.Enabled).ToList();
                            if (admin.Count > 0)
                            {
                                AppUser.RoleId = admin[0].RoleId;
                                AppUser.RoleName = admin[0].RoleName;
                            }
                        }
                    }

                }

            }
        }
    }

    public event Action? OnChange;

    public GeolocationPosition GetGeolocationPosition()
    {
        return _position ?? default!;
    }

    public GeolocationPositionError GetGeolocationPositionError()
    {
        return _positionError ?? default!;
    }

    public string GetCurrency()
    {
        var countryProvider = new CountryProvider();
        var countryInfo = countryProvider.GetCountryByName(AppUser.HomeCountry);

        if (AppUser != default)
        {
            if (countryInfo is { })
            {
                if (countryInfo.Currencies.Count() > 0)
                {
                    return countryInfo.Currencies.FirstOrDefault()?.IsoCode ?? string.Empty;
                }

            }
        }

        return string.Empty;
    }

    /*                                                      ------ Business Logics ------                                                       */

    // TODO:// business logics

    public bool IsCanCreateLoan()
    {
        // TODO: to be implemented
        return true;
    }

    public bool IsLesseeOfThisLoan(LoanDto Loan)
    {
        bool isAssignedLesseeOnThisLoan = false;
        bool isLessee = false;

        if (AppUser != default)
        {
            if (AppUser.RoleName != default)
            {
                if (AppUser.RoleName.Equals("Lessee"))
                {
                    isLessee = true;
                }
            }

            // check if an applicant
            if (Loan != default)
            {
                if (Loan.LoanLessees != default)
                {
                    var loanLessee = Loan.LoanLessees.FirstOrDefault(la => la.LesseeId.Equals(AppUser.Id)) ?? default;
                    if (loanLessee != default)
                    {
                        isAssignedLesseeOnThisLoan = true;
                    }
                }
            }

            if (isLessee && isAssignedLesseeOnThisLoan)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsLesseeCanApply(LoanDto Loan)
    {
        bool isAnApplicantOfThisLoan = false;
        bool isLessee = false;
        // bool isApplicantFlagNormal = false;

        if (AppUser != default)
        {
            if (AppUser.RoleName != default)
            {
                if (AppUser.RoleName.Equals("Lessee"))
                {
                    isLessee = true;
                }
            }

            // check if an applicant
            if (Loan != default)
            {
                if (Loan.LoanApplicants != default)
                {
                    var applicant = Loan.LoanApplicants.FirstOrDefault(la => la.AppUserId.Equals(AppUser.Id)) ?? default;
                    if (applicant != default)
                    {
                        isAnApplicantOfThisLoan = true;
                    }
                }
            }

            // check if all the amount loaned is below package limit
            // don't calculate the payment
            // just the ballpark/basetotal from each loan is enough
            float packageLimit = 1000f;
            float amountTotalLoanedTotal = 100f;
            if (amountTotalLoanedTotal < packageLimit)
            {

            }

            // check if all the amount loaned is below package limit
            int packageLenderLimit = 2;
            int lenderLimitTotal = 1;
            if (lenderLimitTotal < packageLenderLimit)
            {

            }

            if (isLessee && !isAnApplicantOfThisLoan)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsLenderCanUpdateLoan(LoanDto Loan)
    {
        bool isTheLenderOfThisLoan = false;
        bool isLender = false;

        if (AppUser != default)
        {
            if (AppUser.RoleName != default)
            {
                if (AppUser.RoleName.Equals("Lender"))
                {
                    isLender = true;
                }
            }

            // check if THE lender
            if (Loan != default)
            {
                if (Loan.LoanLenders != default)
                {
                    var lender = Loan.LoanLenders.FirstOrDefault(la => la.LenderId.Equals(AppUser.Id)) ?? default;
                    if (lender != default)
                    {
                        isTheLenderOfThisLoan = true;
                    }
                }
            }

            // check if all the amount loaned is below package limit
            // don't calculate the payment
            // just the ballpark/basetotal from each loan is enough
            float packageLimit = 1000f;
            float amountTotalLoanedTotal = 100f;
            if (amountTotalLoanedTotal < packageLimit)
            {

            }

            // check if all the amount loaned is below package limit
            int packageLenderLimit = 2;
            int lenderLimitTotal = 1;
            if (lenderLimitTotal < packageLenderLimit)
            {

            }

            if (isLender && isTheLenderOfThisLoan)
            {
                return true;
            }
        }

        return false;
    }

    public async Task RevalidateVerification(AppUserDto overrideAppUser = default!)
    {
        // if appuser is provided
        // this process can be used by admins
        // revalidating user
        if (overrideAppUser != default)
        {
            AppUser = overrideAppUser;
        }

        if (AppUser != default)
        {
            // appuser get uses the main platform application user identification
            if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.GetAsync(AppUser.ApplicationUserId), Snackbar, null) is AppUserDto appUser)
            {
                if (appUser != default)
                {
                    // revalidate the verification status of the user
                    // stages that needs fulfillment
                    // a. roles and packages
                    // b. documents
                    // c. address
                    // await AppDataService.RevalidateVerification(AppDataService.AppUser.ApplicationUserId);

                    // appuser get uses the main platform application user identification
                    if (appUser.DocumentsStatus.Equals(VerificationStatus.Verified) && appUser.RolePackageStatus.Equals(VerificationStatus.Verified) && appUser.AddressStatus.Equals(VerificationStatus.Verified))
                    {
                        var updateAppUserRequest = new UpdateAppUserRequest
                        {
                            Id = AppUser.Id,
                            ApplicationUserId = AppUser.ApplicationUserId,
                            IsVerified = true
                        };

                        // appuser update uses the extended app user mapping id
                        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.UpdateAsync(AppUser.Id, updateAppUserRequest), Snackbar, null) is Guid guid)
                        {
                            // appuser get uses the main platform application user identification
                            // update the the service appuser info
                            AppUser = await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.GetAsync(AppUser.ApplicationUserId), Snackbar, null) ?? default!;
                        }
                    }
                }
            }
        }
    }

    /*                                                      ------ /Business Logics ------                                                       */

    [JSInvokable]
    public void OnPositionRecieved(GeolocationPosition position)
    {
        _position = position;
        NotifyDataChanged();
    }

    [JSInvokable]
    public void OnPositionError(GeolocationPositionError positionError)
    {
        _positionError = positionError;
        NotifyDataChanged();

    }

    private void NotifyDataChanged() => OnChange?.Invoke();
}
