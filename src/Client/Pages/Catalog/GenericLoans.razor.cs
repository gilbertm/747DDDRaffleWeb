using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Components.EntityTable;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Shared;
using EHULOG.WebApi.Shared.Authorization;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Nager.Country;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog;

public partial class GenericLoans
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthorizationService AuthService { get; set; } = default!;
    [Inject]
    protected IUsersClient UsersClient { get; set; } = default!;
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
    protected ILoanLedgersClient LoanLedgersClient { get; set; } = default!;
    [Inject]
    protected AppDataService AppDataService { get; set; } = default!;

    protected EntityServerTableContext<LoanDto, Guid, LoanViewModel> Context { get; set; } = default!;

    // private readonly EntityTable<LoanDto, Guid, LoanViewModel> _table = default!;

    // private List<ForUploadFile> ForUploadFiles { get; set; } = new();

    private List<AppUserProductDto> AppUserProducts { get; set; } = default!;

    private readonly CustomValidation? _customValidation;

    private string Currency { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await AppDataService.InitializationAsync();
        await LoadContext();

    }

    private async Task LoadContext()
    {
        if (AppDataService != default)
        {
            if (AppDataService.AppUser is not null)
            {
                // for lessee, direct to the front loans
                if ((new string[] { "Lessee" }).Contains(AppDataService.AppUser.RoleName))
                {
                    Navigation.NavigateTo("/");
                }

                if (!string.IsNullOrEmpty(AppDataService.AppUser.HomeCountry))
                {
                    var countryProvider = new CountryProvider();
                    var countryInfo = countryProvider.GetCountryByName(AppDataService.AppUser.HomeCountry);

                    if (countryInfo is { })
                    {
                        if (countryInfo.Currencies.Length > 0)
                        {
                            Currency = countryInfo.Currencies.FirstOrDefault()?.IsoCode ?? string.Empty;
                        }
                    }
                }

                AppUserProducts = (await AppUserProductsClient.GetByAppUserIdAsync(AppDataService.AppUser.Id)).ToList();

                if (AppUserProducts.Count > 0)
                {
                    foreach (var item in AppUserProducts)
                    {
                        if (item.Product is not null)
                        {
                            var image = await InputOutputResourceClient.GetAsync(item.Product.Id);

                            if (image.Count > 0)
                            {
                                item.Product.Image = image.First();
                            }
                        }
                    }
                }

                Context = new(
                       entityName: L["Loan"],
                       entityNamePlural: L["Loans"],
                       entityResource: EHULOGResource.Loans,
                       enableAdvancedSearch: false,
                       fields: new()
                       {
                        new(loan => loan.Id, L["Id"], Template: LoanTemplate),
                        new(loan => loan.StartOfPayment, L["Loan"], "StartOfPayment", Template: LoanDetailsTemplate),

                        // new(loan => loan.Id, L["Status"], Template: LoanStatusTemplate),
                        new(loan => loan.Id, L["Audience"], Template: LoanApplicantsTemplate),

                        // new(loan => loan.LoanLenders?.Where(l => l.LoanId.Equals(loan.Id) && l.LenderId.Equals(_appUserDto.Id)).FirstOrDefault()?.ProductId, L["ProductId"], "LoanLenders.ProductId"),
                        new(loan => loan.Id, L["Ledger"], Template: LoanLedgersTemplate),
                       },
                       idFunc: loanLender => loanLender.Id,
                       searchFunc: async filter =>
                       {
                           var loanFilter = filter.Adapt<SearchLoansRequest>();

                           if (AppDataService.AppUser is not null && !string.IsNullOrEmpty(AppDataService.AppUser.RoleName) && AppDataService.AppUser.RoleName.Equals("Lender"))
                           {
                               loanFilter.LenderId = AppDataService.AppUser.Id;
                               loanFilter.IsLender = true;
                               loanFilter.IsLedger = true;
                               loanFilter.IsLessee = false;
                           }
                           else if (AppDataService.AppUser is not null && !string.IsNullOrEmpty(AppDataService.AppUser.RoleName) && AppDataService.AppUser.RoleName.Equals("Lessee"))
                           {
                               loanFilter.LesseeId = AppDataService.AppUser.Id;
                               loanFilter.IsLender = false;
                               loanFilter.IsLessee = true;
                               loanFilter.IsLedger = true;
                           }
                           else if (AppDataService.AppUser is not null && !string.IsNullOrEmpty(AppDataService.AppUser.RoleName) && AppDataService.AppUser.RoleName.Equals("Admin"))
                           {
                               loanFilter.IsLender = true;
                               loanFilter.IsLessee = true;
                               loanFilter.IsLedger = true;
                           }

                           var result = await LoansClient.SearchAsync(loanFilter);

                           foreach (var item in result.Data)
                           {
                               if (AppDataService.AppUser is { })
                               {
                                   var loanLender = item.LoanLenders?.Where(l => l.LoanId.Equals(item.Id) && l.LenderId.Equals(AppDataService.AppUser.Id)).FirstOrDefault();

                                   if (!string.IsNullOrEmpty(AppDataService.AppUser.RoleName) && AppDataService.AppUser.RoleName.Equals("Admin"))
                                   {
                                       loanLender = item.LoanLenders?.Where(l => l.LoanId.Equals(item.Id)).FirstOrDefault();
                                   }

                                   if (loanLender is { } && loanLender.Lender is { })
                                   {
                                       var userLenderDetailsDto = await UsersClient.GetByIdAsync(loanLender.Lender.ApplicationUserId);

                                       loanLender.Lender.Email = userLenderDetailsDto.Email;
                                       loanLender.Lender.FirstName = userLenderDetailsDto.FirstName;
                                       loanLender.Lender.LastName = userLenderDetailsDto.LastName;
                                       loanLender.Lender.PhoneNumber = userLenderDetailsDto.PhoneNumber;
                                   }

                                   if (loanLender is not null && loanLender.Product is not null)
                                   {
                                       loanLender.Product.Image = AppUserProducts.First(ap => ap.ProductId.Equals(loanLender.ProductId))?.Product?.Image;
                                   }
                               }

                               if (item.LoanApplicants is { })
                               {
                                   if (item.LoanApplicants.Count > 0)
                                   {
                                       foreach (var loanApplicantDto in item.LoanApplicants)
                                       {
                                           var userDetailsDto = await UsersClient.GetByIdAsync(loanApplicantDto.AppUser.ApplicationUserId);

                                           loanApplicantDto.AppUser.FirstName = userDetailsDto.FirstName;
                                           loanApplicantDto.AppUser.LastName = userDetailsDto.LastName;
                                           loanApplicantDto.AppUser.Email = userDetailsDto.Email;
                                           loanApplicantDto.AppUser.PhoneNumber = userDetailsDto.PhoneNumber;
                                       }
                                   }
                               }

                               if (item.LoanLessees is { })
                               {
                                   if (item.LoanLessees.Count > 0)
                                   {
                                       foreach (var loanLesseeDto in item.LoanLessees)
                                       {
                                           var userDetailsDto = await UsersClient.GetByIdAsync(loanLesseeDto.Lessee.ApplicationUserId);

                                           if (loanLesseeDto.Lessee != default)
                                           {
                                               loanLesseeDto.Lessee.FirstName = userDetailsDto.FirstName;
                                               loanLesseeDto.Lessee.LastName = userDetailsDto.LastName;
                                               loanLesseeDto.Lessee.Email = userDetailsDto.Email;
                                               loanLesseeDto.Lessee.PhoneNumber = userDetailsDto.PhoneNumber;
                                           }
                                       }
                                   }
                               }
                           }

                           return result.Adapt<PaginationResponse<LoanDto>>();
                       },
                       createFunc: async loan =>
                       {
                           if (loan.ProductId == Guid.Empty || loan.ProductId.Equals(default))
                           {
                               Snackbar.Add("Product is required.", Severity.Error);
                               throw new FormatException("Product is required.");
                           }

                           // TODO://
                           // can only create if, packages is still allows
                           // Business logic of the number allowed
                           // loans that can be created
                           // amount
                           // etc.
                           var createLoanRequest = loan.Adapt<CreateLoanRequest>();

                           if (await ApiHelper.ExecuteCallGuardedAsync(
                               async () => await LoansClient.CreateAsync(createLoanRequest),
                               Snackbar,
                               _customValidation) is Guid loanId)
                           {
                               if (loanId != Guid.Empty && loanId != default!)
                               {
                                   var createLoanLenderRequest = new CreateLoanLenderRequest()
                                   {
                                       LenderId = AppDataService.AppUser.Id,
                                       LoanId = loanId,
                                       ProductId = loan.ProductId
                                   };

                                   if (await ApiHelper.ExecuteCallGuardedAsync(
                                       async () => await LoanLendersClient.CreateAsync(createLoanLenderRequest),
                                       Snackbar,
                                       _customValidation) is Guid loanLenderId)
                                   {
                                       if (loanLenderId != Guid.Empty && loanLenderId != default!)
                                       {
                                           var createLoanLedgerRequest = new CreateLoanLedgerRequest()
                                           {
                                               LoanId = loanId
                                           };

                                           if (await ApiHelper.ExecuteCallGuardedAsync(
                                               async () => await LoanLedgersClient.CreateAsync(createLoanLedgerRequest),
                                               Snackbar,
                                               _customValidation) is Guid loanLedgerId)
                                           {
                                               if (loanLedgerId != Guid.Empty && loanLedgerId != default!)
                                               {
                                                   Snackbar.Add(L["Loan successfully created."], Severity.Success);

                                                   Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
                                               }
                                           }
                                       }
                                   }
                               }
                           }
                       },
                       editFormInitializedFunc: async () =>
                       {
                           await Task.Run(async () =>
                           {
                               var loanLender = Context.AddEditModal.RequestModel.LoanLenders?.Where(l => l.LoanId.Equals(Context.AddEditModal.RequestModel.Id) && l.LenderId.Equals(AppDataService.AppUser.Id)).FirstOrDefault();

                               _disabledInterestSelection = Context.AddEditModal.RequestModel.InterestType.Equals(InterestType.Zero);

                               if (loanLender is not null)
                               {
                                   Context.AddEditModal.RequestModel.ProductId = default!;
                                   Context.AddEditModal.RequestModel.ProductId = loanLender.ProductId;

                                   if (loanLender.Product is not null)
                                   {
                                       Context.AddEditModal.RequestModel.Product = new();
                                       Context.AddEditModal.RequestModel.Product.Image = new();
                                       Context.AddEditModal.RequestModel.Product.Brand = new();
                                       Context.AddEditModal.RequestModel.Product.Category = new();

                                       Context.AddEditModal.RequestModel.Product = loanLender.Product;
                                       Context.AddEditModal.RequestModel.Product.Id = loanLender.Product.Id;
                                       Context.AddEditModal.RequestModel.Product.Name = loanLender.Product.Name;
                                       Context.AddEditModal.RequestModel.Product.BrandId = loanLender.Product.BrandId;
                                       Context.AddEditModal.RequestModel.Product.CategoryId = loanLender.Product.CategoryId;
                                       Context.AddEditModal.RequestModel.Product.Amount = loanLender.Product.Amount;
                                       Context.AddEditModal.RequestModel.Product.Description = loanLender.Product.Description;

                                       if (loanLender.Product.Image is not null)
                                       {
                                           Context.AddEditModal.RequestModel.Product.Image = loanLender.Product.Image;
                                       }

                                       if (loanLender.Product.Category is not null)
                                       {
                                           Context.AddEditModal.RequestModel.Product.Category = loanLender.Product.Category;
                                       }

                                       if (loanLender.Product.Brand is not null)
                                       {
                                           Context.AddEditModal.RequestModel.Product.Brand = loanLender.Product.Brand;
                                       }
                                   }
                               }

                               await HandleInterest();
                           });
                       },
                       updateFunc: async (id, loan) =>
                       {
                           // Restriction:
                           // Prevent unnecessary loan changes
                           // updating a product is not permitted

                           // TODO://
                           // can only create if, packages is still allows
                           // Business logic of the number allowed
                           // loans that can be created
                           // amount
                           // etc.

                           // CAN ONLY BE CHANGED, if the criteria of allowed limits is still okay
                           // this loan is not yet a running loan
                           // that there's already a lessee assigned.
                           var updateLoanRequest = loan.Adapt<UpdateLoanRequest>();

                           if (await ApiHelper.ExecuteCallGuardedAsync(
                              async () => await LoansClient.UpdateAsync(id, updateLoanRequest),
                              Snackbar,
                              _customValidation) is Guid loanId)
                           {
                               if (id.Equals(loanId))
                               {
                                   var createLoanLedgerRequest = new CreateLoanLedgerRequest()
                                   {
                                       LoanId = loanId
                                   };

                                   if (await ApiHelper.ExecuteCallGuardedAsync(
                                       async () => await LoanLedgersClient.CreateAsync(createLoanLedgerRequest),
                                       Snackbar,
                                       _customValidation) is Guid loanLedgerId)
                                   {
                                       if (loanLedgerId != Guid.Empty && loanLedgerId != default!)
                                       {
                                           Snackbar.Add(L["Loan successfully updated."], Severity.Success);

                                           Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
                                       }
                                   }
                               }

                           }

                           Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
                       },
                       canUpdateEntityFunc: LoanDto =>
                       {
                           if (new[] { LoanStatus.Draft, LoanStatus.Published }.Contains(LoanDto.Status))
                           {
                               if (LoanDto.LoanLessees is { })
                               {
                                   if (LoanDto.LoanLessees.Count > 0)
                                   {
                                       return false;
                                   }
                               }

                               return true;
                           }

                           return false;
                       },
                       canDeleteEntityFunc: LoanDto =>
                       {
                           if (new[] { LoanStatus.Draft, LoanStatus.Published }.Contains(LoanDto.Status))
                           {
                               if (LoanDto.LoanLessees is { })
                               {
                                   if (LoanDto.LoanLessees.Count > 0)
                                   {
                                       return false;
                                   }
                               }

                               return true;
                           }

                           return false;

                       },
                       exportAction: string.Empty,
                       hasExtraActionsFunc: () =>
                       {
                           return true;
                       }
                       );
            }
        }

    }


    private async Task OnClickApproveChildCallBack()
    {
        Context = default!;

        await LoadContext();
    }
}

public class LoanViewModel : UpdateLoanRequest
{
    public Guid ProductId { get; set; }

    public ProductDto Product { get; set; } = new();
}