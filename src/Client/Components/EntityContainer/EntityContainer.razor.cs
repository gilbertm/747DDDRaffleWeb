using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using static EHULOG.WebApi.Shared.Multitenancy.MultitenancyConstants;

namespace EHULOG.BlazorWebAssembly.Client.Components.EntityContainer;

public partial class EntityContainer<TEntity>
{
    [Parameter]
    public EntityContainerContext<TEntity> Context { get; set; } = default!;

    public bool Loading { get; set; }

    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthorizationService AuthService { get; set; } = default!;
    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;

    private IEnumerable<TEntity>? _entityList;
    private int _totalItems;
    private int _pageSize;
    private int _currentPage;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        if (!Loading && Context.SearchFunc is not null)
        {
            Loading = true;

            var filter = GetPaginationFilter();

            if (await ApiHelper.ExecuteCallGuardedAsync(
                    () => Context.SearchFunc(filter), Snackbar)
                is { } result)
            {
                _totalItems = result.TotalCount;
                _entityList = result.Data;
                _pageSize = result.PageSize;
            }

            Loading = false;
        }
    }

    private PaginationFilter GetPaginationFilter()
    {
        StringValues pageCount;

        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("page", out pageCount))
        {
            _currentPage = Convert.ToInt32(pageCount);
        }

        _currentPage = (_currentPage <= 1) ? 1 : _currentPage;

        var filter = new PaginationFilter
        {
            PageNumber = _currentPage,
            PageSize = 12
        };

        return filter;
    }

    protected async override Task OnAfterRenderAsync(bool firstRender) => await JS.InvokeVoidAsync("dotNetJS.containerGrid");
}