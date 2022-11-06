using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;
using System.Collections.Generic;
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

    [Parameter]
    public List<ProductDto> Products { get; set; } = default!;

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

    private async Task<IEnumerable<Guid>> SearchProducts(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            return Products.Where(p => (new string[] { p.Id.ToString().ToLower(), (!string.IsNullOrEmpty(p.Name) ? p.Name.ToLower() : string.Empty), (!string.IsNullOrEmpty(p.Description) ? p.Description.ToLower() : string.Empty), p.CategoryId.ToString().ToLower(), (!string.IsNullOrEmpty(p.Category?.Name) ? p.Category.Name.ToLower() : string.Empty), (!string.IsNullOrEmpty(p.Category?.Description) ? p.Category.Description.ToLower() : string.Empty) }).Contains(value.ToLower())).Select(x => x.Id);
        }

        return Products.Select(x => x.Id);
    }

    private string GetProductName(Guid id) =>
        Products.Find(b => b.Id == id)?.Name ?? string.Empty;

    private RenderFragment SetProductDropdownTemplate(Guid id) => builder =>
    {
        ProductDto product = Products.Where(p => p.Id.Equals(id)).First();

        if (product.Image is null)
            builder.AddMarkupContent(2, string.Format("<table><tr><td class='pr-3'><h3>{0}</h3><span style='font-size:10px'>Category: </span>{1}, <span style='font-size:10px'>Brand: </span>{2}</td><td><h3>{3}</h3></td></tr></table>", product.Name, product.Category?.Name, product.Brand?.Name, product.Amount));
        else
            builder.AddMarkupContent(2, string.Format("<table><tr><td class='pr-3'><img src='{0}' style='height:50px' /></td><td class='pr-3'><h3>{1}</h3><span style='font-size:10px'>Category: </span>{2}, <span style='font-size:10px'>Brand: </span>{3}</td><td class='pr-3'><h3>{4}</h3></td></tr></table>", Config[ConfigNames.ApiBaseUrl] + product.Image.ImagePath, product.Name, product.Category?.Name, product.Brand?.Name, product.Amount));
    };

}