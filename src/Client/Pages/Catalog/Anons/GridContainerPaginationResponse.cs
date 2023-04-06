namespace RAFFLE.BlazorWebAssembly.Client.Pages.Catalog.Anons;

public class GridContainerPaginationResponse<T>
{
    public List<T> Data { get; set; } = default!;
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}
