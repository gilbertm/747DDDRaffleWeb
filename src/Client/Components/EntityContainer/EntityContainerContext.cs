using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.WebApi.Shared.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Components.EntityContainer;

/// <summary>
/// Abstract base class for the initialization Context of the EntityGrid Component.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
public class EntityContainerContext<TEntity>
{
    /// <summary>
    /// A function that loads the specified page from the api with the specified search criteria
    /// and returns a PaginatedResult of TEntity.
    /// </summary>
    public Func<PaginationFilter, Task<EntityContainerPaginationResponse<TEntity>>> SearchFunc { get; }

    public RenderFragment<TEntity> Template { get; set; }

    public EntityContainerContext(Func<PaginationFilter, Task<EntityContainerPaginationResponse<TEntity>>> searchFunc, RenderFragment<TEntity> template)
    {
        SearchFunc = searchFunc;
        Template = template;
    }
}