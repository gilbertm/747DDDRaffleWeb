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

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Administer.User;

public partial class FilesDocuments
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
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    protected EntityServerTableContext<InputOutputResourceDto, Guid, UpdateInputOutputResourceByIdRequest> Context { get; set; } = default!;

    protected override void OnInitialized() =>
        Context = new(
            entityName: L["InputOutputResource"],
            entityNamePlural: L["InputOutputResources"],
            entityResource: EHULOGResource.InputOutputResources,
            fields: new()
            {

                new(resource => resource.Id, L["IO Resource"], "Id", Template: InputOutputResourceDtoTemplate),
                new(resource => resource.Id, L["Id"], "Id", Template: InputOutputResourceDtoIdTemplate)
            },
            idFunc: resource => resource.Id,
            searchFunc: async filter =>
            {
                var searchInputOutputResourcesFilter = filter.Adapt<SearchInputOutputResourcesRequest>();

                searchInputOutputResourcesFilter.ResourceType = InputOutputResourceType.Identification;
                searchInputOutputResourcesFilter.StatusType = InputOutputResourceStatusType.Enabled; // all submitted for verification are enabled by the submit verification on document uploads


                var result = await InputOutputResourceClient.SearchAsync(searchInputOutputResourcesFilter);

                return result.Adapt<PaginationResponse<InputOutputResourceDto>>();
            },
            updateFunc: async (id, resource) =>
            {
                var updatePackageRequest = resource.Adapt<UpdateInputOutputResourceByIdRequest>();

                Guid resourceId = await InputOutputResourceClient.UpdateAsync(id, updatePackageRequest);

                var image = await InputOutputResourceClient.GetAsync(resourceId);

                // NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            },
            searchAction: string.Empty,
            createAction: string.Empty,
            deleteAction: string.Empty,
            exportAction: string.Empty);
}