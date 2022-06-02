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

public partial class Brands
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
    protected IBrandsClient BrandsClient { get; set; } = default!;
    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    protected EntityServerTableContext<BrandDto, Guid, BrandViewModel> Context { get; set; } = default!;

    protected override void OnInitialized() =>
        Context = new(
            entityName: L["Brand"],
            entityNamePlural: L["Brands"],
            entityResource: EHULOGResource.Brands,
            fields: new()
            {
                new(brand => brand.Id, L["Id"], "Id", Template: BrandDtoTemplate),
                new(brand => brand.Name, L["Name"], "Name"),
                new(brand => brand.Description, L["Description"], "Description"),
            },
            idFunc: brand => brand.Id,
            searchFunc: async filter =>
            {
                var result = await BrandsClient.SearchAsync(filter.Adapt<SearchBrandsRequest>());

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

                return result.Adapt<PaginationResponse<BrandDto>>();
            },
            createFunc: async brand =>
            {
                var createBrandRequestGuid = await BrandsClient.CreateAsync(brand.Adapt<CreateBrandRequest>());

                if (!string.IsNullOrEmpty(brand.ImageInBytes))
                {
                    var imageCreate = await InputOutputResourceClient.CreateAsync(new CreateInputOutputResourceRequest()
                    {
                        ReferenceId = createBrandRequestGuid,
                        InputOutputResourceDocumentType = InputOutputResourceDocumentType.None,
                        Image = new FileUploadRequest()
                        {
                            Data = brand.ImageInBytes ?? default!,
                            Extension = brand.ImageExtension ?? string.Empty,
                            Name = $"{brand.Name}_{Guid.NewGuid():N}"
                        },
                        InputOutputResourceStatusType = InputOutputResourceStatusType.Disabled,
                        InputOutputResourceType = InputOutputResourceType.Brand
                    });
                }

                brand.ImageInBytes = string.Empty;

                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            },
            updateFunc: async (id, brand) =>
            {
                var updateBrandRequest = brand.Adapt<UpdateBrandRequest>();

                Guid brandId = await BrandsClient.UpdateAsync(id, updateBrandRequest);

                var image = await InputOutputResourceClient.GetAsync(brandId);

                if (!string.IsNullOrEmpty(brand.ImageInBytes))
                {
                    var deleteImage = await InputOutputResourceClient.DeleteAsync(brand.Id);

                    if (!string.IsNullOrEmpty(deleteImage.ToString()))
                    {
                        var updateImage = await InputOutputResourceClient.CreateAsync(new CreateInputOutputResourceRequest()
                        {
                            ReferenceId = brandId,
                            InputOutputResourceDocumentType = InputOutputResourceDocumentType.None,
                            Image = new FileUploadRequest()
                            {
                                Data = brand.ImageInBytes ?? default!,
                                Extension = brand.ImageExtension ?? string.Empty,
                                Name = $"{brand.Name}_{Guid.NewGuid():N}"
                            },
                            InputOutputResourceStatusType = InputOutputResourceStatusType.Disabled,
                            InputOutputResourceType = InputOutputResourceType.Brand
                        });
                    }
                }

                brand.ImageInBytes = string.Empty;

                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            },
            deleteFunc: async id =>
            {
                var deleteBrandId = await BrandsClient.DeleteAsync(id);

                if (deleteBrandId != default!)
                {
                    var deleteImage = await InputOutputResourceClient.DeleteAsync(deleteBrandId);
                }
            },
            exportAction: string.Empty);

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

public class BrandViewModel : UpdateBrandRequest
{
    public InputOutputResourceDto? Image { get; set; }

    public string? ImagePath { get; set; }
    public string? ImageInBytes { get; set; }
    public string? ImageExtension { get; set; }
}