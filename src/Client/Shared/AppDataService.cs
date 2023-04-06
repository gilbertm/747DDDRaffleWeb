using Blazored.LocalStorage;
using RAFFLE.BlazorWebAssembly.Client.Components.EntityTable;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.Common;
using Mapster;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using MudBlazor;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Nager.Country;

namespace RAFFLE.BlazorWebAssembly.Client.Shared;

public class AppDataService : IAppDataService
{
    private ILogger Logger { get; set; } = default!;

    private IGeolocationService GeolocationService { get; set; } = default!;

    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private IHttpClientFactory HttpClientFactory { get; set; } = default!;

    private IConfiguration Configuration { get; set; } = default!;

    private IAppUsersClient AppUsersClient { get; set; } = default!;

    private IPackagesClient PackagesClient { get; set; } = default!;

    private IRolesClient RolesClient { get; set; } = default!;

    private ILoansClient LoansClient { get; set; } = default!;

    private IUsersClient UsersClient { get; set; } = default!;

    private ISubscriptionsClient SubscriptionsClient { get; set; } = default!;

    private ILocalStorageService LocalStorageService { get; set; } = default!;

    private ISnackbar Snackbar { get; set; } = default!;

    public AppDataService(ILoggerFactory loggerFactory, ILocalStorageService localStorageService, IGeolocationService geolocationService, AuthenticationStateProvider authenticationStateProvider, ISnackbar snackbar,  IConfiguration configuration, IAppUsersClient appUsersClient, IPackagesClient packagesClient, IUsersClient usersClient, IRolesClient rolesClient, IHttpClientFactory httpClientFactory, ILoansClient loansClient, ISubscriptionsClient subscriptionsClient)
    {
        Logger = loggerFactory.CreateLogger($"RaffleConsoleWriteLine - {nameof(AppDataService)}");

        LocalStorageService = localStorageService;

        GeolocationService = geolocationService;

        HttpClientFactory = httpClientFactory;

        Snackbar = snackbar;

        Configuration = configuration;

        AppUsersClient = appUsersClient;

        UsersClient = usersClient;

        PackagesClient = packagesClient;

        RolesClient = rolesClient;

        AuthenticationStateProvider = authenticationStateProvider;

        LoansClient = loansClient;

        SubscriptionsClient = subscriptionsClient;

        GeolocationService.GetCurrentPosition(
           component: this,
           onSuccessCallbackMethodName: nameof(OnPositionRecieved),
           onErrorCallbackMethodName: nameof(OnPositionError),
           options: _options);

        ShowValuesAppDto();
    }

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
    public bool ErrorPopupProfile { get; set; } = false; // use this for error, if true make an error popup

    private bool Changed { get; set; } = false;

    private GeolocationPosition? _position;

    private GeolocationPositionError? _positionError;

    public void ShowValuesAppDto()
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            var st = new StackTrace(new StackFrame(1));

            if (st != default)
            {
                if (st.GetFrame(0) != default)
                {
                    Logger.LogInformation($"RaffleConsoleWriteLine {st?.GetFrame(0)?.GetMethod()?.Name} - Country: {JsonSerializer.Serialize(Country)}");
                    Logger.LogInformation($"RaffleConsoleWriteLine {st?.GetFrame(0)?.GetMethod()?.Name} - City: {JsonSerializer.Serialize(City)}");
                    Logger.LogInformation($"RaffleConsoleWriteLine {st?.GetFrame(0)?.GetMethod()?.Name} - AppUser: {JsonSerializer.Serialize(AppUser)}");
                }
            }
        }
    }

    public async Task InitializationAsync()
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            var st = new StackTrace(new StackFrame(1));

            if (st != default)
            {
                if (st.GetFrame(0) != default)
                {
                    Logger.LogInformation($"RaffleConsoleWriteLine {st?.GetFrame(0)?.GetMethod()?.Name} - InitializationAsync");
                }
            }
        }

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

                            Changed = false;
                        }
                    }
                }
            }

            // role is defined
            // get current subscription package
            // assign if no subscription
            if (!string.IsNullOrEmpty(AppUser.RoleId))
            {
                await GetCurrentUserPackageAsync(true);

                if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.GetApplicationUserAsync(userId), Snackbar, null) is AppUserDto appUserUpdated)
                {
                    AppUser = appUserUpdated;
                }
            }
        }

        // if (_position == default)
           //  await UpdateLocationAsync();
    }

    private async Task<AppUserDto> SetupAppUser(string userId)
    {
        try
        {
            var appUserCheck = await AppUsersClient.GetApplicationUserAsync(userId);

        }
        catch (Exception)
        {

            var guid = await AppUsersClient.CreateAsync(new CreateAppUserRequest
            {
                ApplicationUserId = userId
            });

            if (guid != default)
            {
                var createSubscriptonRequest = new CreateSubscriptionRequest
                {
                    AppUserId = guid
                };

                var guidCreated = await SubscriptionsClient.CreateAsync(createSubscriptonRequest);

                if (guidCreated == default)
                {
                    ErrorPopupProfile = true;

                    Console.WriteLine("RaffleConsoleWriteLine -- AppService / Setup AppUser / Contact Administrator");
                }
            }

        }

        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.GetApplicationUserAsync(userId), Snackbar, default) is AppUserDto appUser)
        {
            if (appUser != default)
            {
                return appUser;
            }

        }

        return default!;
    }

    /// <summary>
    /// Updates the location of the app user
    /// OnAfterRenderAsync is the most recommended
    /// </summary>
    /// <returns></returns>
    [Obsolete("UpdateLocationAsync looks unnecessary.")]
    public async Task UpdateLocationAsync()
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            var st = new StackTrace(new StackFrame(1));

            if (st != default)
            {
                if (st.GetFrame(0) != default)
                {
                    Logger.LogInformation($"RaffleConsoleWriteLine {st?.GetFrame(0)?.GetMethod()?.Name} - {JsonSerializer.Serialize(AppUser)} - Update Location");
                }
            }
        }

        if (_position != default)
        {
            if (_position.Coords != default)
            {
                if (AppUser != default)
                {
                    if (AppUser.Longitude == default && AppUser.Latitude == default)
                    {
                        AppUser.Longitude = _position.Coords.Longitude.ToString();
                        AppUser.Latitude = _position.Coords.Latitude.ToString();

                    }

                    var updateAppUserRequest = new UpdateAppUserRequest
                    {
                        Id = AppUser.Id,
                        ApplicationUserId = AppUser.ApplicationUserId,
                        HomeAddress = AppUser.HomeAddress,
                        HomeCity = AppUser.HomeCity,
                        HomeCountry = AppUser.HomeCountry,
                        HomeRegion = AppUser.HomeRegion,
                        Latitude = AppUser.Latitude,
                        Longitude = AppUser.Longitude,
                    };

                    if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.UpdateAsync(AppUser.Id, updateAppUserRequest), Snackbar, null) is Guid guidUpdate)
                    {
                        if (guidUpdate != default && guidUpdate != Guid.Empty)
                        {
                            if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.GetApplicationUserAsync(AppUser.ApplicationUserId), Snackbar, null) is AppUserDto appUserUpdated)
                            {
                                AppUser = appUserUpdated;
                            }
                        }
                    }
                }
            }

        }
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
    public async Task<PackageDto> GetCurrentUserPackageAsync(bool assignDefaultIfMissingPackageInSubscription = false)
    {
        if (AppUser != default)
        {
            if (AppUser.RoleName != default)
            {
                if (AppUser.Subscription != default)
                {
                    if (AppUser.Subscription.PackageId != default)
                    {
                        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await PackagesClient.GetAsync(AppUser.Subscription.PackageId), Snackbar) is List<PackageDto> packages)
                        {
                            if (packages != default && packages.Count > 0)
                                return packages.First();
                        }
                    }
                    else
                    {
                        if (assignDefaultIfMissingPackageInSubscription)
                            await AssignDefaultPackageAsync(AppUser.RoleName);
                    }
                }
            }
        }

        return default!;
    }

    private async Task AssignDefaultPackageAsync(string roleName)
    {
        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await PackagesClient.GetAsync(null), Snackbar) is List<PackageDto> packages)
        {
            if (packages != default)
            {
                switch (roleName)
                {
                    case "Lender":

                        UpdateSubscriptionRequest updateSubscriptionRequestLender = new()
                        {
                            AppUserId = AppUser.Id,
                            PackageId = packages.First(p => p.IsDefault && p.IsLender).Id
                        };

                        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await SubscriptionsClient.UpdateAsync(AppUser.Id, updateSubscriptionRequestLender), Snackbar) is Guid subscriptionIdLender)
                        {
                            if (subscriptionIdLender != default)
                            {
                                // default package assigned
                                // update the verification status
                                UpdateAppUserRequest updateAppUserRequestLender = new()
                                {
                                    Id = AppUser.Id,
                                    ApplicationUserId = AppUser.ApplicationUserId,
                                    RolePackageStatus = VerificationStatus.Verified
                                };

                                Guid guidLender = await AppUsersClient.UpdateAsync(AppUser.Id, updateAppUserRequestLender);

                                if (guidLender != default)
                                {
                                    await RevalidateVerification();
                                }
                            }
                        }

                        break;

                    case "Lessee":

                        UpdateSubscriptionRequest updateSubscriptionRequestLessee = new()
                        {
                            AppUserId = AppUser.Id,
                            PackageId = packages.First(p => p.IsDefault && !p.IsLender).Id
                        };

                        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await SubscriptionsClient.UpdateAsync(AppUser.Id, updateSubscriptionRequestLessee), Snackbar) is Guid subscriptionIdLessee)
                        {
                            if (subscriptionIdLessee != default)
                            {
                                // default package assigned
                                // update the verification status
                                UpdateAppUserRequest updateAppUserRequestLessee = new()
                                {
                                    Id = AppUser.Id,
                                    ApplicationUserId = AppUser.ApplicationUserId,
                                    RolePackageStatus = VerificationStatus.Verified
                                };

                                Guid guidLessee = await AppUsersClient.UpdateAsync(AppUser.Id, updateAppUserRequestLessee);

                                if (guidLessee != default)
                                {
                                    await RevalidateVerification();
                                }
                            }
                        }

                        break;
                }

            }
        }
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