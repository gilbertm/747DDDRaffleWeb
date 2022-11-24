using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Mapster;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.WebApi.Shared.Multitenancy;
using System.Net.Http.Headers;
using System.Text.Json;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Anons;

public partial class AnonymousLoanGridContainer
{
    [Parameter]
    public IEnumerable<LoanDto> EntityList { get; set; } = default!;

    [Parameter]
    public int TotalItems { get; set; }

    [Parameter]
    public int PageSize { get; set; }

    [Parameter]
    public int CurrentPage { get; set; }

    [Parameter]
    public IDictionary<Guid, bool> DictOverlayVisibility { get; set; } = default!;

    private void IsVisibleProductOverlay(Guid guid)
    {
        foreach (var item in DictOverlayVisibility)
        {
            DictOverlayVisibility[item.Key] = false;
        }

        if (DictOverlayVisibility.ContainsKey(guid))
            DictOverlayVisibility[guid] = !DictOverlayVisibility[guid];

        StateHasChanged();
    }

    private void CloseVisibleProductOverlay(Guid guid)
    {
        if (DictOverlayVisibility.ContainsKey(guid))
            DictOverlayVisibility[guid] = false;

        StateHasChanged();
    }
}