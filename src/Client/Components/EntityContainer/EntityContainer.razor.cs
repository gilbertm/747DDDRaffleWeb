using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Microsoft.JSInterop;

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

    private IEnumerable<TEntity>? _entityList;
    private int _totalItems;

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
            }

            Loading = false;
        }
    }

    private PaginationFilter GetPaginationFilter()
    {
        var filter = new PaginationFilter
        {
        };

        return filter;
    }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        await JS.InvokeVoidAsync("dotNetJS.containerGrid");
    }
}