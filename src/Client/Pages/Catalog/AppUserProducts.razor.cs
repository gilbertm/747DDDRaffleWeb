using System.Security.Claims;
using EHULOG.BlazorWebAssembly.Client.Components.EntityTable;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Shared;
using EHULOG.WebApi.Shared.Authorization;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog;

public partial class AppUserProducts
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthorizationService AuthService { get; set; } = default!;
    [Inject]
    public AppDataService AppDataService { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    protected IAppUserProductsClient AppUserProductsClient { get; set; } = default!;

    [Inject]
    protected IProductsClient ProductsClient { get; set; } = default!;

    [Inject]
    protected IAppUsersClient AppUsersClient { get; set; } = default!;

    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    protected EntityServerTableContext<AppUserProductDto, Guid, AppUserProductViewModel> Context { get; set; } = default!;

    private AppUserDto _appUserDto;

    protected override async Task OnInitializedAsync()
    {
        _appUserDto = await AppDataService.Start();

        // consolidate, add all system products, default products
        // on appuser products, so that the user can make use of them in making loans
        var appUserProducts = (await AppUserProductsClient.GetByAppUserIdAsync(_appUserDto.Id)).ToList();

        foreach (var appUserProduct in appUserProducts)
        {
            if (appUserProduct.Id == default)
            {
                _ = await AppUserProductsClient.CreateAsync(new()
                {
                    AppUserId = _appUserDto.Id,
                    ProductId = appUserProduct.Product?.Id ?? default!
                });
            }
        }

        // manage all available products
        // get all active system products that the lender can use
        Context = new(
               entityName: L["AppUser Product"],
               entityNamePlural: L["AppUser Products"],
               entityResource: EHULOGResource.AppUserProducts,
               fields: new()
               {
                        new(prod => prod.Id, L["Product"], Template: AppUserIdFieldTemplate),
                        new(prod => prod.Product?.Brand?.Name, L["Brand"], "Product.Brand.Name"),
                        new(prod => prod.Product?.Category?.Name, L["Category"], "Product.Category.Name"),
                        new(prod => prod.Product?.ProductType, L["Type"], "Product.ProductType"),
               },
               idFunc: prod => prod.Id,
               searchFunc: async filter =>
               {
                   // only the following can be listed
                   // a. system provided products
                   // b. custom products, possible of package subscription
                   var productFilter = filter.Adapt<SearchAppUserProductsRequest>();

                   productFilter.AppUserId = _appUserDto.Id;

                   productFilter.ProductId = null;

                   var result = await AppUserProductsClient.SearchAsync(productFilter);

                   if (result.Data.Count() > 0)
                   {
                       foreach (var item in result.Data)
                       {
                           var image = await InputOutputResourceClient.GetAsync(item.ProductId);

                           if (image.Count() > 0)
                           {
                               item.Product.Image = image.First();
                           }
                       }
                   }

                   return result.Adapt<PaginationResponse<AppUserProductDto>>();
               },
               createFunc: async prod =>
               {
                   var state = await AuthState;
                   bool _canCreate = await CanDoActionAsync(Context.CreateAction, state);

                   if (!_canCreate && !prod.Product.ProductType.Equals(ProductType.Custom))
                   {
                       throw new Exception("You are authorized to access this resource. Testing");
                   }

                   prod.AppUserId = _appUserDto.Id;

                   /* TODO: if with package to allow or can be able to create custom product */
                   /* product type = 3 custom for lender, admins must be any */
                   /* need to check the role */
                   /* only lenders (with subscription package) and system adminis can create products */
                   var createProductRequest = prod.Product.Adapt<CreateProductRequest>();

                   if (_appUserDto.RoleName is not null && _appUserDto.RoleName.Equals("Lender"))
                   {
                       createProductRequest.ProductType = ProductType.Custom;
                   }

                   Guid productId = await ProductsClient.CreateAsync(createProductRequest);
                   prod.ProductId = productId;


                   if (!string.IsNullOrEmpty(prod.ImageInBytes))
                   {
                       var imageCreate = await InputOutputResourceClient.CreateAsync(new CreateInputOutputResourceRequest()
                       {
                           ReferenceId = prod.ProductId,
                           InputOutputResourceDocumentType = InputOutputResourceDocumentType.None,
                           Image = new FileUploadRequest()
                           {
                               Data = prod.ImageInBytes ?? default!,
                               Extension = prod.ImageExtension ?? string.Empty,
                               Name = $"{prod.Product.Name}_{Guid.NewGuid():N}"
                           },
                           InputOutputResourceStatusType = InputOutputResourceStatusType.Disabled,
                           InputOutputResourceType = InputOutputResourceType.Product
                       });
                   }

                   var createAppUserProductRequest = prod.Adapt<CreateAppUserProductRequest>();
                   Guid appUserProductId = await AppUserProductsClient.CreateAsync(createAppUserProductRequest);
                   prod.Id = appUserProductId;
                   prod.ImageInBytes = string.Empty;

                   NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
               },
               updateFunc: async (id, prod) =>
               {
                   var updateProductRequest = prod.Product.Adapt<UpdateProductRequest>();
                   Guid productId = await ProductsClient.UpdateAsync(prod.ProductId, updateProductRequest);
                   prod.ProductId = productId;

                   var image = await InputOutputResourceClient.GetAsync(prod.ProductId);

                   if (!string.IsNullOrEmpty(prod.ImageInBytes))
                   {
                       var deleteImage = await InputOutputResourceClient.DeleteAsync(prod.ProductId);

                       if (!string.IsNullOrEmpty(deleteImage.ToString()))
                       {
                           var updateImage = await InputOutputResourceClient.CreateAsync(new CreateInputOutputResourceRequest()
                           {
                               ReferenceId = prod.ProductId,
                               InputOutputResourceDocumentType = InputOutputResourceDocumentType.None,
                               Image = new FileUploadRequest()
                               {
                                   Data = prod.ImageInBytes ?? default!,
                                   Extension = prod.ImageExtension ?? string.Empty,
                                   Name = $"{prod.Product.Name}_{Guid.NewGuid():N}"
                               },
                               InputOutputResourceStatusType = InputOutputResourceStatusType.Disabled,
                               InputOutputResourceType = InputOutputResourceType.Product
                           });
                       }
                   }

                   var updateAppUserProductRequest = prod.Adapt<UpdateAppUserProductRequest>();
                   await AppUserProductsClient.UpdateAsync(id, updateAppUserProductRequest);
                   prod.ImageInBytes = string.Empty;

                   NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
               },
               deleteFunc: async id =>
               {
                   var deleteAppUserProduct = await AppUserProductsClient.GetIdAsync(id);

                   if (deleteAppUserProduct != null && deleteAppUserProduct != default!)
                   {
                       var deleteImage = await InputOutputResourceClient.DeleteAsync(deleteAppUserProduct.ProductId);

                       var deleteProduct = await ProductsClient.DeleteAsync(deleteAppUserProduct.ProductId);

                       if (!string.IsNullOrEmpty(deleteProduct.ToString()))
                       {
                           await AppUserProductsClient.DeleteAsync(deleteAppUserProduct.AppUserId, deleteAppUserProduct.ProductId);
                       }
                   }

                   NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
               },
               canUpdateEntityFunc: prod =>
               {
                   bool canUpdate = false;

                   if (_appUserDto is not null && !string.IsNullOrEmpty(_appUserDto.RoleName))
                   {
                       if (_appUserDto.RoleName.Equals("Admin"))
                       {
                           return true;
                       }
                       else if (_appUserDto.RoleName.Equals("Lender"))
                       {
                           if (prod != null)
                           {
                               if (prod.Product is not null && prod.Product.ProductType.Equals(ProductType.Custom))
                               {
                                   canUpdate = true;
                               }
                           }
                       }
                   }

                   return canUpdate;
               },
               canDeleteEntityFunc: prod =>
               {
                   bool canDelete = false;

                   if (_appUserDto is not null && !string.IsNullOrEmpty(_appUserDto.RoleName))
                   {
                       if (_appUserDto.RoleName.Equals("Admin"))
                       {
                           return true;
                       }
                       else if (_appUserDto.RoleName.Equals("Lender"))
                       {
                           if (prod != null)
                           {
                               if (prod.Product is not null && prod.Product.ProductType.Equals(ProductType.Custom))
                               {
                                   canDelete = true;
                               }
                           }
                       }
                   }

                   return canDelete;
               },
               searchAction: ((Func<string>)(() =>
               {
                   return string.Empty;
               }))(),
               exportAction: ((Func<string>)(() => {
                   return string.Empty;
               }))()
               );
    }

    private async Task<bool> CanDoActionAsync(string? action, AuthenticationState state) =>
    !string.IsNullOrWhiteSpace(action) &&
        ((bool.TryParse(action, out bool isTrue) && isTrue) || // check if action equals "True", then it's allowed
        (Context.EntityResource is { } resource && await AuthService.HasPermissionAsync(state.User, action, resource)));

    public void ClearImageInBytes()
    {
        Context.AddEditModal.RequestModel.ImageInBytes = string.Empty;
        Context.AddEditModal.ForceRender();
    }

    public void SetDeleteCurrentImageFlag()
    {
        Context.AddEditModal.RequestModel.ImageInBytes = string.Empty;
        Context.AddEditModal.RequestModel.ImagePath = string.Empty;
        // Context.AddEditModal.RequestModel.DeleteCurrentImage = true;
        Context.AddEditModal.ForceRender();
    }

    // TODO : Make this as a shared service or something? Since it's used by Profile Component also for now, and literally any other component that will have image upload.
    // The new service should ideally return $"data:{ApplicationConstants.StandardImageFormat};base64,{Convert.ToBase64String(buffer)}"
    private async Task UploadFiles(InputFileChangeEventArgs e)
    {
        if (e.File != null)
        {
            string? extension = Path.GetExtension(e.File.Name);
            if (!ApplicationConstants.SupportedImageFormats.Contains(extension.ToLower()))
            {
                Snackbar.Add("Image Format Not Supported.", Severity.Error);
                return;
            }

            Context.AddEditModal.RequestModel.ImageExtension = extension;
            var imageFile = await e.File.RequestImageFileAsync(ApplicationConstants.StandardImageFormat, ApplicationConstants.MaxImageWidth, ApplicationConstants.MaxImageHeight);
            byte[]? buffer = new byte[imageFile.Size];
            await imageFile.OpenReadStream(ApplicationConstants.MaxAllowedSize).ReadAsync(buffer);
            Context.AddEditModal.RequestModel.ImageInBytes = $"data:{ApplicationConstants.StandardImageFormat};base64,{Convert.ToBase64String(buffer)}";
            Context.AddEditModal.ForceRender();
        }
    }
}

public class AppUserProductViewModel : UpdateAppUserProductRequest
{
    public Guid AppUserId { get; set; }
    public Guid ProductId { get; set; }
    public ProductDto Product { get; set; } = new();

    public AppUser AppUser { get; set; } = new();

    public string? ImagePath { get; set; }
    public string? ImageInBytes { get; set; }
    public string? ImageExtension { get; set; }
}