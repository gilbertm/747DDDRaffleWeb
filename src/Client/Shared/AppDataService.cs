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
using Mapster;
using EHULOG.BlazorWebAssembly.Client.Components.EntityTable;

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

        Console.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
        Console.WriteLine("------------------------------------ AppDataService loaded... ------------------------------------");
        Console.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
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

    public string? City { get; set; }
    public string? Country { get; set; }
    public string? CountryCurrency { get; set; }

    private bool Changed { get; set; } = false;

    private GeolocationPosition? _position;

    private GeolocationPositionError? _positionError;

    public async Task InitializationAsync()
    {
        GeolocationService.GetCurrentPosition(
                   component: this,
                   onSuccessCallbackMethodName: nameof(OnPositionRecieved),
                   onErrorCallbackMethodName: nameof(OnPositionError),
                   options: _options);

        var userClaimsPrincipal = await IsAuthenticated();

        if (userClaimsPrincipal == default)
        {
            return;
        }

        string userId = userClaimsPrincipal.GetUserId() ?? string.Empty;
        string email = userClaimsPrincipal.GetEmail() ?? string.Empty;
        string firstName = userClaimsPrincipal.GetFirstName() ?? string.Empty;
        string lastName = userClaimsPrincipal.GetSurname() ?? string.Empty;
        string phoneNumber = userClaimsPrincipal.GetPhoneNumber() ?? string.Empty;
        string imageUrl = string.IsNullOrEmpty(userClaimsPrincipal?.GetImageUrl()) ? string.Empty : (Configuration[ConfigNames.ApiBaseUrl] + userClaimsPrincipal?.GetImageUrl());

        // if not found / not found exception
        // create a silent custom helper
        // the app user should be present here
        AppUser = await SetupAppUser(userId);

        if (AppUser != default)
        {
            if (string.IsNullOrEmpty(AppUser.Email))
            {
                AppUser.Email = email;
                AppUser.FirstName = firstName;
                AppUser.LastName = lastName;
                AppUser.PhoneNumber = phoneNumber;
                AppUser.ImageUrl = imageUrl;

                Changed = true;
            }

            if (string.IsNullOrEmpty(AppUser.RoleId))
            {
                var userRoles = await UsersClient.GetRolesAsync(userId);

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

                Changed = true;
            }

            if (Changed)
            {
                if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.UpdateAsync(AppUser.Id, AppUser.Adapt<UpdateAppUserRequest>()), Snackbar, null) is Guid guidUpdate)
                {
                    if (guidUpdate != default && guidUpdate != Guid.Empty)
                    {
                        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.GetApplicationUserAsync(userId), Snackbar, null) is AppUserDto appUserUpdated)
                        {
                            AppUser = appUserUpdated;
                        }
                    }
                }
            }
        }
    }

    private async Task<AppUserDto> SetupAppUser(string userId)
    {
        var appUserObjectAlreadyCreatedDetail = await ApiHelper.ExecuteCallGuardedCustomSuppressAsync(async () => await AppUsersClient.GetApplicationUserAsync(userId), Snackbar, default);

        // the appuser detail record
        // does not exists
        if (appUserObjectAlreadyCreatedDetail == default)
        {
            var createAppUserRequest = new CreateAppUserRequest
            {
                ApplicationUserId = userId
            };

            if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.CreateAsync(createAppUserRequest), Snackbar, null, "Personal application profile created") is Guid guidCreated)
            {
                if (guidCreated != default && guidCreated != Guid.Empty)
                {
                    Console.WriteLine("EhulogConsoleWriteLine: Personal application profile created");

                    if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.GetApplicationUserAsync(userId), Snackbar, null, "Init Geolocation") is AppUserDto userDetail)
                    {
                        Console.WriteLine("EhulogConsoleWriteLine: Init Geolocation");

                        if (_position != default)
                        {
                            userDetail.Longitude = _position.Coords.Longitude.ToString();
                            userDetail.Latitude = _position.Coords.Latitude.ToString();

                            var responseReverseGeocoding = await MapBoxGeocoding.ReverseGeocodingAsync(new()
                            {
                                Coordinate = new Geo.MapBox.Models.Coordinate()
                                {
                                    Latitude = Convert.ToDouble(userDetail.Latitude),
                                    Longitude = Convert.ToDouble(userDetail.Longitude)
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
                                                                userDetail.HomeCountry = c.ContextText[0].Text;
                                                                break;
                                                            case string s when s.Contains("region"):
                                                                userDetail.HomeRegion = c.ContextText[0].Text;
                                                                break;
                                                            case string s when s.Contains("postcode"):
                                                                userDetail.HomeAddress += c.ContextText[0].Text;
                                                                break;
                                                            case string s when s.Contains("district"):
                                                                userDetail.HomeCountry += c.ContextText[0].Text;
                                                                break;
                                                            case string s when s.Contains("place"):
                                                                userDetail.HomeCity = c.ContextText[0].Text;
                                                                break;
                                                            case string s when s.Contains("locality"):
                                                                userDetail.HomeAddress += c.ContextText[0].Text;
                                                                break;
                                                            case string s when s.Contains("neighborhood"):
                                                                userDetail.HomeAddress += c.ContextText[0].Text;
                                                                break;
                                                            case string s when s.Contains("address"):
                                                                userDetail.HomeAddress += c.ContextText[0].Text;
                                                                break;
                                                            case string s when s.Contains("poi"):
                                                                userDetail.HomeAddress += c.ContextText[0].Text;
                                                                break;
                                                        }
                                                    }
                                                }
                                            }

                                            if (f.Center is { })
                                            {
                                                userDetail.Latitude = f.Center.Latitude.ToString();
                                                userDetail.Longitude = f.Center.Longitude.ToString();
                                            }

                                            if (f.Properties.Address is { })
                                            {
                                                userDetail.HomeAddress += f.Properties.Address;
                                            }
                                        }
                                    }

                                }
                            }


                            var updateAppUserRequest = new UpdateAppUserRequest
                            {
                                Id = userDetail.Id,
                                ApplicationUserId = userDetail.ApplicationUserId,
                                HomeAddress = userDetail.HomeAddress,
                                HomeCity = userDetail.HomeCity,
                                HomeCountry = userDetail.HomeCountry,
                                HomeRegion = userDetail.HomeRegion,
                                Latitude = userDetail.Latitude,
                                Longitude = userDetail.Longitude,
                            };

                            if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.UpdateAsync(userDetail.Id, updateAppUserRequest), Snackbar, null) is Guid guidUpdate)
                            {
                                if (guidUpdate != default && guidUpdate != Guid.Empty)
                                {
                                    if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.GetApplicationUserAsync(userId), Snackbar, null) is AppUserDto appUserUpdated)
                                    {
                                        if (appUserUpdated != default)
                                            return appUserUpdated;
                                    }
                                }
                            }
                        }


                    }
                }
            }

        }

        return appUserObjectAlreadyCreatedDetail ?? default!;
    }

    public async Task<ClaimsPrincipal> IsAuthenticated()
    {
        // note: front anon requests has bypass on the jwt handler
        // Client.Infrastructure\Auth\Jwt\JwtAuthenticationHeaderHandler.cs
        // this allows custom passthrough
        // equivalent
        // var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        // var user = authState.User;
        // IsAuthenticated = user.Identity?.IsAuthenticated ?? false;

        var getAuthenticationStateAsync = await AuthenticationStateProvider.GetAuthenticationStateAsync();

        if (getAuthenticationStateAsync != default)
        {
            if ((await AuthenticationStateProvider.GetAuthenticationStateAsync()).User is ClaimsPrincipal userClaimsPrincipal)
            {
                if (userClaimsPrincipal.Identity != default)
                {
                    if (userClaimsPrincipal.Identity.IsAuthenticated)
                    {
                        return userClaimsPrincipal;
                    }
                    else
                    {
                        return default!;
                    }
                }
            }
        }

        return default!;
    }

    public string GetCurrencyAppUser()
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

    public event Action? OnChange;

    public GeolocationPosition GetGeolocationPosition()
    {
        return _position ?? default!;
    }

    public GeolocationPositionError GetGeolocationPositionError()
    {
        return _positionError ?? default!;
    }

    #region business logics

    /// <summary>
    /// Applicable: Lender
    /// Check the current user's package
    ///     against lent or provided running loans (draft, published, running payment)
    /// </summary>
    /// <returns>bool</returns>
    public async Task<bool> CanCreateActionAsync()
    {
        if (AppUser != default)
        {
            if (AppUser.RoleName != default)
            {
                if (AppUser.RoleName.Equals("Lender"))
                {
                    // get package
                    var package = await GetCurrentUserPackageAsync();

                    // get package
                    var loans = await GetCurrentUserLoansAsync();

                    if (package != default)
                    {

                        float currentLentTotal = 0;

                        if (loans != default && loans.Count > 0)
                        {
                            foreach (var loan in loans)
                            {
                                if (loan.LoanLenders != default && loan.LoanLenders.FirstOrDefault(ll => ll.LenderId.Equals(AppUser.Id)) != default)
                                {
                                    LoanLenderDto loanLender = loan.LoanLenders.FirstOrDefault(ll => ll.LenderId.Equals(AppUser.Id)) ?? default!;

                                    if (loanLender != default && loanLender.Product != default)
                                    {
                                        currentLentTotal += loanLender.Product.Amount;
                                    }
                                }
                            }

                            if (!(loans.Count < package.MaxLessees))
                                return false;

                        }

                        if (!(currentLentTotal < package.MaxAmounts))
                            return false;



                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Applicable: Lessee
    /// Check the current user's package
    ///     against applied loans (awarded, running)
    /// </summary>
    /// <returns>bool</returns>
    public async Task<bool> CanApplyActionAsync()
    {
        if (AppUser != default)
        {
            if (AppUser.RoleName != default)
            {
                if (AppUser.RoleName.Equals("Lessee"))
                {
                    // get package
                    var package = await GetCurrentUserPackageAsync();

                    // get package
                    var loans = await GetCurrentUserLoansAsync();

                    if (package != default && loans != default)
                    {

                        float currentAppliedTotal = 0;

                        if (loans.Count > 0)
                        {
                            foreach (var loan in loans)
                            {
                                if (loan.LoanLessees != default && loan.LoanLessees.FirstOrDefault(ll => ll.LesseeId.Equals(AppUser.Id)) != default)
                                {
                                    LoanLesseeDto loanLessee = loan.LoanLessees.FirstOrDefault(ll => ll.LesseeId.Equals(AppUser.Id)) ?? default!;

                                    // loan lessee is good to go
                                    // lessee is awarded with this loan
                                    if (loanLessee != default)
                                    {
                                        // get the product thru lender
                                        if (loan.LoanLenders != default && loan.LoanLessees.FirstOrDefault() != default)
                                        {
                                            LoanLenderDto loanLender = loan.LoanLenders.FirstOrDefault() ?? default!;

                                            if (loanLender != default && loanLender.Product != default)
                                            {

                                                currentAppliedTotal += loanLender.Product.Amount;
                                            }
                                        }
                                    }
                                }
                            }

                        }

                        if (!(currentAppliedTotal < package.MaxAmounts))
                            return false;

                        if (!(loans.Count < package.MaxLessees))
                            return false;

                        return true;
                    }
                }
            }
        }

        return false;
    }

    #endregion

    #region helpers

    /// <summary>
    /// Get current user's package
    /// </summary>
    /// <returns>PackageDto package</returns>
    public async Task<PackageDto> GetCurrentUserPackageAsync()
    {
        if (AppUser != default)
        {
            if (AppUser.RoleName != default)
            {
                if (await ApiHelper.ExecuteCallGuardedAsync(async () => await PackagesClient.GetAsync(AppUser.PackageId), Snackbar) is List<PackageDto> packages)
                {
                    if (packages != default && packages.Count() > 0)
                        return packages.First();
                }
            }
        }

        return default!;
    }

    public async Task<List<LoanDto>> GetCurrentUserLoansAsync(bool runningLoans = false)
    {
        if (AppUser != default)
        {
            if (AppUser.RoleName != default)
            {
                if (new string[] { "Lender", "Lessee" }.Contains(AppUser.RoleName))
                {
                    if (AppUser.RoleName.Equals("Lender"))
                    {
                        SearchLoansRequest searchLoanRequest = new SearchLoansRequest
                        {
                            LenderId = AppUser.Id,
                            IsLender = true,
                            IsLedger = true,
                            IsLessee = false,
                            Statuses = new[] { LoanStatus.Draft, LoanStatus.Published, LoanStatus.Assigned, LoanStatus.Meetup, LoanStatus.Payment, LoanStatus.PaymentFinal, LoanStatus.Finish, LoanStatus.Rate }

                        };

                        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await LoansClient.SearchAsync(searchLoanRequest), Snackbar) is PaginationResponseOfLoanDto loans)
                        {
                            var resultLoans = loans.Adapt<PaginationResponse<LoanDto>>();

                            if (resultLoans != default && resultLoans.Data.Count() > 0)
                            {
                                return resultLoans.Data.OrderByDescending(l => l.StartOfPayment).OrderByDescending(l => l.Status).ToList();
                            }
                        }
                    }

                    if (AppUser.RoleName.Equals("Lessee"))
                    {
                        LoanStatus[] statuses = new[] { LoanStatus.Published, LoanStatus.Assigned, LoanStatus.Meetup, LoanStatus.Payment, LoanStatus.PaymentFinal, LoanStatus.Finish, LoanStatus.Rate };

                        if (runningLoans)
                        {
                            statuses = new[] { LoanStatus.Assigned, LoanStatus.Meetup, LoanStatus.Payment, LoanStatus.PaymentFinal, LoanStatus.Finish, LoanStatus.Rate };
                        }

                        SearchLoansLesseeRequest searchLoansLesseeRequest = new SearchLoansLesseeRequest
                        {
                            AppUserId = AppUser.Id,
                            HomeCity = AppUser.HomeCity,
                            HomeCountry = AppUser.HomeCountry,
                            Statuses = statuses

                        };

                        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await LoansClient.SearchLesseeAsync(searchLoansLesseeRequest), Snackbar) is PaginationResponseOfLoanDto loans)
                        {
                            var resultLoans = loans.Adapt<PaginationResponse<LoanDto>>();

                            if (resultLoans != default && resultLoans.Data.Count() > 0)
                            {
                                return resultLoans.Data.OrderByDescending(l => l.StartOfPayment).OrderByDescending(l => l.Status).ToList();
                            }
                        }
                    }

                }
            }
        }

        return default!;
    }

    public bool HasRatedHelper(LoanDto Loan)
    {
        if (AppUser != default)
        {
            if (AppUser.RoleName != default)
            {
                if (Loan != default)
                {
                    if (Loan.Ratings != default)
                    {
                        if (Loan.Ratings.Count > 0)
                        {
                            if (Loan.Ratings.First().LenderId != default || Loan.Ratings.First().LesseeId != default)
                            {
                                if (Loan.Ratings.First().LenderId != default && AppUser.Id.Equals(Loan.Ratings.First().LenderId))
                                {
                                    return true;
                                }

                                if (Loan.Ratings.First().LesseeId != default && AppUser.Id.Equals(Loan.Ratings.First().LesseeId))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    public bool HasRatedBothLenderLesseeHelper(LoanDto Loan)
    {
        bool lenderVoted = false;
        bool lesseeVoted = false;

        if (AppUser != default)
        {
            if (AppUser.RoleName != default)
            {
                if (Loan != default)
                {
                    if (Loan.Ratings != default)
                    {
                        if (Loan.Ratings.Count > 0)
                        {
                            if (Loan.Ratings.First().LenderId != default || Loan.Ratings.First().LesseeId != default)
                            {
                                if (Loan.Ratings.First().LenderId != default)
                                {
                                    lenderVoted = true;
                                }

                                if (Loan.Ratings.First().LesseeId != default)
                                {
                                    lesseeVoted = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        if (lenderVoted && lesseeVoted)
            return true;

        return false;
    }

    public bool IsLesseeOfLoan(LoanDto Loan)
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

    public bool IsLenderOfLoan(LoanDto Loan)
    {
        if (AppUser != default)
        {
            if (AppUser.RoleName != default)
            {
                if (AppUser.RoleName.Equals("Lender"))
                {
                    if (Loan != default)
                    {
                        if (Loan.LoanLenders != default)
                        {
                            var loanLender = Loan.LoanLenders.FirstOrDefault(la => la.LenderId.Equals(AppUser.Id)) ?? default;

                            if (loanLender != default)
                            {
                                return true;
                            }
                        }
                    }

                }
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
            isTheLenderOfThisLoan = IsLenderOfLoan(Loan);

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

    public async Task<bool> IsVerified(AppUserDto overrideAppUser = default!)
    {
        // if appuser is provided
        // this process can be used by admins
        // revalidating user
        if (overrideAppUser != default)
        {
            AppUser = overrideAppUser;
        }

        // just paranoia check to ensure that it is revalidated and accurate until this point
        await RevalidateVerification(AppUser);

        if (AppUser != default)
        {
            // appuser get uses the main platform application user identification
            if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.GetAsync(AppUser.Id), Snackbar, null) is AppUserDto appUser)
            {
                if (appUser != default)
                {
                    if (appUser.DocumentsStatus.Equals(VerificationStatus.Verified) && appUser.AddressStatus.Equals(VerificationStatus.Verified) && appUser.RolePackageStatus.Equals(VerificationStatus.Verified))
                    {
                        return true;
                    }

                }
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
            if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.GetAsync(AppUser.Id), Snackbar, null) is AppUserDto appUser)
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
                            AppUser = await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.GetAsync(AppUser.Id), Snackbar, null) ?? default!;

                        }
                    }
                }
            }
        }
    }

    #endregion

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