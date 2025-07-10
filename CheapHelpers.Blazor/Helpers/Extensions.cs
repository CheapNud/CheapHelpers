using CheapHelpers.EF;
using CheapHelpers.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Diagnostics;


namespace CheapHelpers.Blazor
{
    public static class Extensions
    {
        public static void ReloadPage(this NavigationManager manager)
        {
            manager.NavigateTo(manager.Uri, true);
        }

        public static Task<TableData<T>> ToTableData<T>(this IQueryable<T> query, TableState state, CancellationToken token = default) where T : class, IEntityId
        {
            return query.ToTableData(state.Page + 1, state.PageSize, token);
        }

        public async static Task<TableData<T>> ToTableData<T>(this IQueryable<T> query, int? pageIndex = null, int pageSize = 10, CancellationToken token = default) where T : class, IEntityId
        {
            var result = await query.ToPaginatedListAsync(pageIndex, pageSize, token);
            return result.ToTableData();
        }

        public static TableData<T> ToTableData<T>(this PaginatedList<T> p) where T : class, IEntityId
        {
            ArgumentNullException.ThrowIfNull(p);
            return new TableData<T>() { TotalItems = p.ResultCount, Items = p };
        }

        public async static Task<PaginatedList<T>> GetAllPaginated<T>(this BaseRepo repo, TableState state, CancellationToken token = default) where T : class, IEntityId
        {
            ArgumentNullException.ThrowIfNull(state);
            return await repo.GetAllPaginated<T>(state.Page + 1, state.PageSize, token);
        }

        public async static Task<TableData<T>> GetAllTableData<T>(this BaseRepo repo, TableState state, CancellationToken token = default) where T : class, IEntityId
        {
            try
            {
                ArgumentNullException.ThrowIfNull(state);
                var result = await repo.GetAllPaginated<T>(state, token);
                return result.ToTableData();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
