using CheapHelpers.Blazor.Shared;
using CheapHelpers.Models.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Linq.Expressions;

namespace MecamApplication.Blazor.Services
{
    /// <summary>
    /// Service to simplify common search dialog operations
    /// </summary>
    public class SearchDialogService(IDialogService dialogService)
    {
        /// <summary>
        /// Generic method to search for any entity type
        /// </summary>
        public async Task<T?> SearchEntityAsync<T>(
            string? title = null,
            string? label = null,
            Func<T, string>? displayProp = null,
            Expression<Func<T, bool>>? where = null,
            Expression<Func<T, object>>? orderBy = null,
            Expression<Func<T, object>>? orderByDescending = null,
            bool showDetails = false,
            RenderFragment<T>? detailsContent = null) where T : class, IEntityCode, new()
        {
            var parameters = new DialogParameters
            {
                ["Title"] = title ?? $"Search {typeof(T).Name}",
                ["Label"] = label ?? typeof(T).Name,
                ["DisplayProp"] = displayProp ?? (x => x.Code),
                ["Where"] = where,
                ["OrderBy"] = orderBy ?? (Expression<Func<T, object>>)(x => x.Code),
                ["OrderByDescending"] = orderByDescending,
                ["ShowDetails"] = showDetails,
                ["DetailsContent"] = detailsContent
            };

            var dialog = await dialogService.ShowAsync<SearchDialog<T>>(
                title ?? "Search", parameters);

            var result = await dialog.Result;

            return !result.Canceled && result.Data is T entity ? entity : default;
        }

        ///// <summary>
        ///// Search for a product
        ///// </summary>
        //public async Task<Product?> SearchProductAsync()
        //{
        //    return await SearchEntityAsync<Product>(
        //        title: "Search Product",
        //        label: "Product",
        //        displayProp: p => $"{p.Code} - {p.Name}",
        //        orderBy: x => x.Name);
        //}

    }
}