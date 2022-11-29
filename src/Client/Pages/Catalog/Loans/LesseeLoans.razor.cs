using EHULOG.BlazorWebAssembly.Client.Components.EntityContainer;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Nager.Country;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans;

public partial class LesseeLoans
{
    [CascadingParameter(Name = "AppDataService")]
    protected AppDataService AppDataService { get; set; } = default!;

    [Inject]
    protected ILoanLendersClient LoanLendersClient { get; set; } = default!;
    [Inject]
    protected IProductsClient ProductsClient { get; set; } = default!;
    [Inject]
    protected IAppUsersClient AppUsersClient { get; set; } = default!;
    [Inject]
    protected IAppUserProductsClient AppUserProductsClient { get; set; } = default!;
    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;
    [Inject]
    protected ILoansClient LoansClient { get; set; } = default!;
    [Inject]
    protected IUsersClient UsersClient { get; set; } = default!;
    [Inject]
    protected ILoanLedgersClient LoanLedgersClient { get; set; } = default!;
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;
    protected EntityContainerContext<LoanDto> Context { get; set; } = default!;

    protected bool Loading { get; set; }

    private List<AppUserProductDto> appUserProducts { get; set; } = default!;

    private bool IsHistory { get; set; }

    private string _currency { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await AppDataService.InitializationAsync();
        LoadContext();
    }

    protected override async Task OnParametersSetAsync()
    {
        IsHistory = false;
        Context = default!;

        if (QueryHelpers.ParseQuery(Navigation.ToAbsoluteUri(Navigation.Uri).Query).TryGetValue("history", out var param))
        {
            string history = param.First();

            // https://localhost:5002/loans/lessee?history=true
            if (history != default && history.Equals("true"))
            {
                IsHistory = true;
            }

        }

        await OnInitializedAsync();
    }

    private void LoadContext()
    {
        if (AppDataService.AppUser != default)
        {
            if (!string.IsNullOrEmpty(AppDataService.AppUser.RoleName) && AppDataService.AppUser.RoleName.Equals("Lender"))
            {
                NavigationManager.NavigateTo("/catalog/all/loans", true);
            }
            else
            {
                if (!string.IsNullOrEmpty(AppDataService.AppUser.HomeCountry))
                {
                    var countryProvider = new CountryProvider();
                    var countryInfo = countryProvider.GetCountryByName(AppDataService.AppUser.HomeCountry);

                    if (countryInfo is { })
                    {
                        if (countryInfo.Currencies.Count() > 0)
                        {
                            _currency = countryInfo.Currencies.FirstOrDefault()?.IsoCode ?? string.Empty;
                        }
                    }
                }

                Context = new EntityContainerContext<LoanDto>(
                           searchFunc: async filter =>
                           {
                               var loanFilter = filter.Adapt<SearchLoansLesseeRequest>();

                               loanFilter.AppUserId = AppDataService.AppUser.Id;

                               if (IsHistory)
                               {
                                   loanFilter.Statuses = new List<LoanStatus>();
                                   loanFilter.Statuses.Add(LoanStatus.RateFinal);
                                   loanFilter.Statuses.Add(LoanStatus.Dispute);
                               }
                               else
                               {
                                   loanFilter.Statuses = new[] { LoanStatus.Published, LoanStatus.Assigned, LoanStatus.Meetup, LoanStatus.Payment, LoanStatus.PaymentFinal, LoanStatus.Finish, LoanStatus.Rate };
                               }

                               var result = await LoansClient.SearchLesseeAsync(loanFilter);

                               foreach (var item in result.Data)
                               {

                                   if (item.LoanLenders != default)
                                   {
                                       if (item.LoanLenders.Count > 0)
                                       {
                                           var loanLender = item.LoanLenders.Where(l => l.LoanId.Equals(item.Id)).FirstOrDefault();

                                           if (loanLender is not null && loanLender.Product is not null)
                                           {
                                               var image = await InputOutputResourceClient.GetAsync(loanLender.ProductId);

                                               if (image.Count() > 0)
                                               {

                                                   loanLender.Product.Image = image.First();
                                               }
                                           }
                                       }
                                   }

                                   if (item.LoanApplicants != default)
                                   {

                                       if (item.LoanApplicants.Count > 0)
                                       {
                                           foreach (var loanApplicant in item.LoanApplicants)
                                           {
                                               if (loanApplicant.AppUser != default)
                                               {
                                                   var userDetailsDto = await UsersClient.GetByIdAsync(loanApplicant.AppUser.ApplicationUserId);

                                                   loanApplicant.AppUser.FirstName = userDetailsDto.FirstName;
                                                   loanApplicant.AppUser.LastName = userDetailsDto.LastName;
                                                   loanApplicant.AppUser.Email = userDetailsDto.Email;
                                                   loanApplicant.AppUser.PhoneNumber = userDetailsDto.PhoneNumber;
                                                   loanApplicant.AppUser.ImageUrl = $"{Config[ConfigNames.ApiBaseUrl]}{userDetailsDto.ImageUrl}";
                                               }
                                           }
                                       }
                                   }
                               }

                               if (result.Data != default && result.Data.Count() > 0)
                                   result.Data = result.Data.OrderByDescending(l => l.StartOfPayment).OrderByDescending(l => l.Status).ToList();

                               return result.Adapt<EntityContainerPaginationResponse<LoanDto>>();
                           },
                           template: BodyTemplate);
            }
        }

    }
}