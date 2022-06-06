using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;
using System.Text;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog;

public class ProductAutocomplete : MudAutocomplete<Guid>
{
    [Inject]
    private IStringLocalizer<ProductAutocomplete> L { get; set; } = default!;
    [Inject]
    private IProductsClient ProductsClient { get; set; } = default!;
    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;
    [Inject]
    protected IConfiguration Config { get; set; } = default!;
    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    private List<ProductDto> _products = new();

    // supply default parameters, but leave the possibility to override them
    public override Task SetParametersAsync(ParameterView parameters)
    {
        Label = L["Product"];
        Variant = Variant.Filled;
        Dense = true;
        Margin = Margin.Dense;
        ResetValueOnEmptyText = true;
        SearchFunc = SearchProducts;
        ToStringFunc = GetProductName;
        Clearable = true;
        ItemTemplate = SetProductDropdownTemplate;
        ItemSelectedTemplate = SetProductDropdownTemplate;
        return base.SetParametersAsync(parameters);
    }

    // when the value parameter is set, we have to load that one product to be able to show the name
    // we can't do that in OnInitialized because of a strange bug (https://github.com/MudBlazor/MudBlazor/issues/3818)
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender &&
            _value != default &&
            await ApiHelper.ExecuteCallGuardedAsync(
                () => ProductsClient.GetAsync(_value), Snackbar) is { } product)
        {
            var image = (await InputOutputResourceClient.GetAsync(product.Id)).FirstOrDefault();
            if (image is not null)
            {
                product.Image = image;
            }

            _products.Add(product);
            ForceRender(true);
        }
    }

    private async Task<IEnumerable<Guid>> SearchProducts(string value)
    {
        var filter = new SearchProductsRequest
        {
            PageSize = 10,
            AdvancedSearch = new() { Fields = new[] { "name" }, Keyword = value }
        };

        if (await ApiHelper.ExecuteCallGuardedAsync(
                () => ProductsClient.SearchAsync(filter), Snackbar)
            is PaginationResponseOfProductDto response)
        {
            foreach (var item in response.Data)
            {
                var image = (await InputOutputResourceClient.GetAsync(item.Id)).FirstOrDefault();
                if (image is not null)
                {
                    item.Image = image;
                }

            }
            _products = response.Data.ToList();
        }

        return _products.Select(x => x.Id);
    }

    private string GetProductName(Guid id) =>
        _products.Find(b => b.Id == id)?.Name ?? string.Empty;

    private RenderFragment SetProductDropdownTemplate(Guid id) => builder =>
    {
        ProductDto product = _products.Where(p => p.Id.Equals(id)).First();

        if (product.Image is null)
            builder.AddMarkupContent(2, string.Format("<table><tr><td class='pr-3'><h3>{0}</h3><span style='font-size:10px'>Category: </span>{1}, <span style='font-size:10px'>Brand: </span>{2}</td><td><h3>{3}</h3></td></tr></table>", product.Name, product.Category?.Name, product.Brand?.Name, product.Amount));
        else
            builder.AddMarkupContent(2, string.Format("<table><tr><td class='pr-3'><img src='{0}' style='height:50px' /></td><td class='pr-3'><h3>{1}</h3><span style='font-size:10px'>Category: </span>{2}, <span style='font-size:10px'>Brand: </span>{3}</td><td class='pr-3'><h3>{4}</h3></td></tr></table>", Config[ConfigNames.ApiBaseUrl] + product.Image.ImagePath, product.Name, product.Category?.Name, product.Brand?.Name, product.Amount));
    };

}