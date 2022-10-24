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

public partial class Category
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
    protected ICategoriesClient CategoriesClient { get; set; } = default!;
    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    protected EntityServerTableContext<CategoryDto, Guid, CategoryViewModel> Context { get; set; } = default!;

    protected override void OnInitialized()
    {
        Context = new(
            entityName: L["Category"],
            entityNamePlural: L["Categories"],
            entityResource: EHULOGResource.Categories,
            fields: new()
            {
                new(category => category.Id, L["Id"], "Id", Template: CategoryDtoTemplate),
                new(category => category.Name, L["Name"], "Name"),
                new(category => category.Description, L["Description"], "Description"),
            },
            idFunc: category => category.Id,
            searchFunc: async filter =>
            {
                var result = await CategoriesClient.SearchAsync(filter.Adapt<SearchCategoriesRequest>());

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

                return result.Adapt<PaginationResponse<CategoryDto>>();
            },
            createFunc: async category =>
            {
                var createCategoryRequestGuid = await CategoriesClient.CreateAsync(category.Adapt<CreateCategoryRequest>());

                if (!string.IsNullOrEmpty(category.ImageInBytes))
                {
                    var imageCreate = await InputOutputResourceClient.CreateAsync(new CreateInputOutputResourceRequest()
                    {
                        ReferenceId = createCategoryRequestGuid,
                        InputOutputResourceDocumentType = InputOutputResourceDocumentType.None,
                        Image = new FileUploadRequest()
                        {
                            Data = category.ImageInBytes ?? default!,
                            Extension = category.ImageExtension ?? string.Empty,
                            Name = $"{category.Name}_{Guid.NewGuid():N}"
                        },
                        InputOutputResourceStatusType = InputOutputResourceStatusType.Disabled,
                        InputOutputResourceType = InputOutputResourceType.Category
                    });
                }

                category.ImageInBytes = string.Empty;

                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            },
            updateFunc: async (id, category) =>
            {
                var updateCategoryRequest = category.Adapt<UpdateCategoryRequest>();

                Guid categoryId = await CategoriesClient.UpdateAsync(id, updateCategoryRequest);

                var image = await InputOutputResourceClient.GetAsync(categoryId);

                if (!string.IsNullOrEmpty(category.ImageInBytes))
                {
                    var deleteImage = await InputOutputResourceClient.DeleteAsync(category.Id);

                    if (!string.IsNullOrEmpty(deleteImage.ToString()))
                    {
                        var updateImage = await InputOutputResourceClient.CreateAsync(new CreateInputOutputResourceRequest()
                        {
                            ReferenceId = categoryId,
                            InputOutputResourceDocumentType = InputOutputResourceDocumentType.None,
                            Image = new FileUploadRequest()
                            {
                                Data = category.ImageInBytes ?? default!,
                                Extension = category.ImageExtension ?? string.Empty,
                                Name = $"{category.Name}_{Guid.NewGuid():N}"
                            },
                            InputOutputResourceStatusType = InputOutputResourceStatusType.Disabled,
                            InputOutputResourceType = InputOutputResourceType.Category
                        });
                    }
                }

                category.ImageInBytes = string.Empty;

                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            },
            deleteFunc: async id =>
            {
                var deleteBrandId = await CategoriesClient.DeleteAsync(id);

                if (deleteBrandId != default!)
                {
                    var deleteImage = await InputOutputResourceClient.DeleteAsync(deleteBrandId);
                }
            },
            exportAction: string.Empty);
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

public class CategoryViewModel : UpdateCategoryRequest
{
    public new InputOutputResourceDto Image { get; set; } = new();

    public string? ImagePath { get; set; }
    public string? ImageInBytes { get; set; }
    public string? ImageExtension { get; set; }
}