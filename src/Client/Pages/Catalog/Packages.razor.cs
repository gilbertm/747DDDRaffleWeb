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

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog;

public partial class Packages
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
    protected IPackagesClient PackagesClient { get; set; } = default!;
    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    protected EntityServerTableContext<PackageDto, Guid, PackageViewModel> Context { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await AppDataService.InitializationAsync();

        if (AppDataService != default)
        {
            if (AppDataService.AppUser != default)
            {

                Context = new(
       entityName: L["Package"],
       entityNamePlural: L["Packages"],
       entityResource: EHULOGResource.Packages,
       fields: new()
       {
                new(package => package.Id, L["Id"], "Id", Template: PackageDtoTemplate),
                new(package => package.Name, L["Features"], "Name", Template: PackageDtoFeaturesTemplate),
                new(package => package.IsLender, L["Lender"], "IsLender"),
       },
       idFunc: package => package.Id,
       searchFunc: async filter =>
       {
           var packageSearchFilter = filter.Adapt<SearchPackagesRequest>();

           if (AppDataService.AppUser.RoleName?.Equals("Lender") == true)
           {
               packageSearchFilter.IsLender = true;
           }
           else if (AppDataService.AppUser.RoleName?.Equals("Lessee") == true)
           {
               packageSearchFilter.IsLender = false;
           }

           var result = await PackagesClient.SearchAsync(packageSearchFilter);

           if (result.Data.Count() > 0)
           {
               foreach (var item in result.Data)
               {
                   var image = await InputOutputResourceClient.GetAsync(item.Id);

                   if (image.Count() > 0)
                   {
                       item.Image = image.First();
                   }
               }
           }

           return result.Adapt<PaginationResponse<PackageDto>>();
       },
       createFunc: async package =>
       {
           var createPackageRequestGuid = await PackagesClient.CreateAsync(package.Adapt<CreatePackageRequest>());

           if (!string.IsNullOrEmpty(package.ImageInBytes))
           {
               var imageCreate = await InputOutputResourceClient.CreateAsync(new CreateInputOutputResourceRequest()
               {
                   ReferenceId = createPackageRequestGuid,
                   InputOutputResourceDocumentType = InputOutputResourceDocumentType.None,
                   Image = new FileUploadRequest()
                   {
                       Data = package.ImageInBytes ?? default!,
                       Extension = package.ImageExtension ?? string.Empty,
                       Name = $"{package.Name}_{Guid.NewGuid():N}"
                   },
                   InputOutputResourceStatusType = InputOutputResourceStatusType.Disabled,
                   InputOutputResourceType = InputOutputResourceType.Package
               });
           }

           package.ImageInBytes = string.Empty;

           NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
       },
       updateFunc: async (id, package) =>
       {
           var updatePackageRequest = package.Adapt<UpdatePackageRequest>();

           Guid packageId = await PackagesClient.UpdateAsync(id, updatePackageRequest);

           var image = await InputOutputResourceClient.GetAsync(packageId);

           if (!string.IsNullOrEmpty(package.ImageInBytes))
           {
               var deleteImage = await InputOutputResourceClient.DeleteAsync(package.Id);

               if (!string.IsNullOrEmpty(deleteImage.ToString()))
               {
                   var updateImage = await InputOutputResourceClient.CreateAsync(new CreateInputOutputResourceRequest()
                   {
                       ReferenceId = packageId,
                       InputOutputResourceDocumentType = InputOutputResourceDocumentType.None,
                       Image = new FileUploadRequest()
                       {
                           Data = package.ImageInBytes ?? default!,
                           Extension = package.ImageExtension ?? string.Empty,
                           Name = $"{package.Name}_{Guid.NewGuid():N}"
                       },
                       InputOutputResourceStatusType = InputOutputResourceStatusType.Disabled,
                       InputOutputResourceType = InputOutputResourceType.Package
                   });
               }
           }

           package.ImageInBytes = string.Empty;

           NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
       },
       deleteFunc: async id =>
       {
           var deletePackageId = await PackagesClient.DeleteAsync(id);

           if (deletePackageId != default!)
           {
               var deleteImage = await InputOutputResourceClient.DeleteAsync(deletePackageId);
           }
       },
       exportAction: string.Empty);
            }
        }
    }

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

public class PackageViewModel : UpdatePackageRequest
{
    public InputOutputResourceDto? Image { get; set; }

    public string? ImagePath { get; set; }
    public string? ImageInBytes { get; set; }
    public string? ImageExtension { get; set; }
}