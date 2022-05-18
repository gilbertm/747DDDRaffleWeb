using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog;

public class CategoryAutocomplete : MudAutocomplete<Guid>
{
    [Inject]
    private IStringLocalizer<CategoryAutocomplete> L { get; set; } = default!;
    [Inject]
    private ICategoriesClient CategoriesClient { get; set; } = default!;
    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private List<CategoryDto> _categories = new();

    // supply default parameters, but leave the possibility to override them
    public override Task SetParametersAsync(ParameterView parameters)
    {
        Label = L["Category"];
        Variant = Variant.Filled;
        Dense = true;
        Margin = Margin.Dense;
        ResetValueOnEmptyText = true;
        SearchFunc = SearchCategories;
        ToStringFunc = GetCategoryName;
        Clearable = true;
        return base.SetParametersAsync(parameters);
    }

    // when the value parameter is set, we have to load that one brand to be able to show the name
    // we can't do that in OnInitialized because of a strange bug (https://github.com/MudBlazor/MudBlazor/issues/3818)
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender &&
            _value != default &&
            await ApiHelper.ExecuteCallGuardedAsync(
                () => CategoriesClient.GetAsync(_value), Snackbar) is { } brand)
        {
            _categories.Add(brand);
            ForceRender(true);
        }
    }

    private async Task<IEnumerable<Guid>> SearchCategories(string value)
    {
        var filter = new SearchCategoriesRequest
        {
            PageSize = 10,
            AdvancedSearch = new() { Fields = new[] { "name" }, Keyword = value }
        };

        if (await ApiHelper.ExecuteCallGuardedAsync(
                () => CategoriesClient.SearchAsync(filter), Snackbar)
            is PaginationResponseOfCategoryDto response)
        {
            _categories = response.Data.ToList();
        }

        return _categories.Select(x => x.Id);
    }

    private string GetCategoryName(Guid id) =>
        _categories.Find(b => b.Id == id)?.Name ?? string.Empty;
}